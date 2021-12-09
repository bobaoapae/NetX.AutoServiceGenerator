using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.IO.Pipelines;
using System.Buffers;
using System.Collections.Generic;
using System;
using NetX.Options;
using System.Collections.Concurrent;
using System.IO;

namespace NetX
{
    public abstract class NetXConnection : INetXConnection
    {
        protected Socket _socket;
        protected CancellationToken _token;
        protected NetXConnectionOptions _options;
       
        private byte[] _sendBuffer;
        private byte[] _recvBuffer;

        private readonly Pipe _sendPipe;
        private readonly Pipe _receivePipe;

        private readonly ConcurrentDictionary<Guid, TaskCompletionSource<ArraySegment<byte>>> _completions;
        private readonly object _sendSync = new();

        const int GUID_LEN = 16;
        private static readonly byte[] _emptyGuid = Guid.Empty.ToByteArray();
        private static readonly ArrayPool<byte> _bufferPool = ArrayPool<byte>.Shared;

        public NetXConnection()
        {
            _sendPipe = new Pipe();
            _receivePipe = new Pipe();
            _completions = new ConcurrentDictionary<Guid, TaskCompletionSource<ArraySegment<byte>>>();
        }

        public async ValueTask SendAsync(ArraySegment<byte> buffer)
        {
            Monitor.Enter(_sendSync);
            try
            {
                _sendPipe.Writer.Write(BitConverter.GetBytes(buffer.Count + (_options.Duplex ? sizeof(int) + GUID_LEN : 0)));

                if (_options.Duplex)
                {
                    _sendPipe.Writer.Write(_emptyGuid);
                }

                Memory<byte> memory = _sendPipe.Writer.GetMemory(buffer.Count);
                buffer.AsMemory().CopyTo(memory);

                _sendPipe.Writer.Advance(buffer.Count);
                
                await _sendPipe.Writer.FlushAsync();
            }
            finally
            {
                Monitor.Exit(_sendSync);
            }
        }

        public async ValueTask SendAsync(Stream stream)
        {
            Monitor.Enter(_sendSync);
            try
            {
                stream.Position = 0;

                _sendPipe.Writer.Write(BitConverter.GetBytes((int)stream.Length + (_options.Duplex ? sizeof(int) + GUID_LEN : 0)));

                if (_options.Duplex)
                {
                    _sendPipe.Writer.Write(_emptyGuid);
                }

                Memory<byte> memory = _sendPipe.Writer.GetMemory((int)stream.Length);
                int bytesRead = await stream.ReadAsync(memory);
                if (bytesRead != 0)
                {
                    _sendPipe.Writer.Advance(bytesRead);
                    _ = await _sendPipe.Writer.FlushAsync();
                }
            }
            finally
            {
                Monitor.Exit(_sendSync);
            }
        }

        public async ValueTask<ArraySegment<byte>> RequestAsync(ArraySegment<byte> buffer)
        {
            if (!_options.Duplex)
                throw new NotSupportedException($"Cannot use RequestAsync with {nameof(_options.Duplex)} option disabled");

            var messageId = Guid.NewGuid();
            if (!_completions.TryAdd(messageId, new TaskCompletionSource<ArraySegment<byte>>()))
                throw new Exception($"Cannot track completion for MessageId = {messageId}");

            Monitor.Enter(_sendSync);
            try
            {
                _sendPipe.Writer.Write(BitConverter.GetBytes(buffer.Count + sizeof(int) + GUID_LEN));

                _sendPipe.Writer.Write(messageId.ToByteArray());

                Memory<byte> memory = _sendPipe.Writer.GetMemory(buffer.Count);
                buffer.AsMemory().CopyTo(memory);

                _sendPipe.Writer.Advance(buffer.Count);

                await _sendPipe.Writer.FlushAsync();
            }
            finally
            {
                Monitor.Exit(_sendSync);
            }

            return await _completions[messageId].Task;
        }

        public async ValueTask<ArraySegment<byte>> RequestAsync(Stream stream)
        {
            if (!_options.Duplex)
                throw new NotSupportedException($"Cannot use RequestAsync with {nameof(_options.Duplex)} option disabled");

            var messageId = Guid.NewGuid();
            if (!_completions.TryAdd(messageId, new TaskCompletionSource<ArraySegment<byte>>()))
                throw new Exception($"Cannot track completion for MessageId = {messageId}");

            Monitor.Enter(_sendSync);
            try
            {
                _sendPipe.Writer.Write(BitConverter.GetBytes((int)stream.Length + sizeof(int) + GUID_LEN));

                _sendPipe.Writer.Write(messageId.ToByteArray());

                Memory<byte> memory = _sendPipe.Writer.GetMemory((int)stream.Length);
                int bytesRead = await stream.ReadAsync(memory);
                if (bytesRead != 0)
                {
                    _sendPipe.Writer.Advance(bytesRead);
                    _ = await _sendPipe.Writer.FlushAsync();
                }
            }
            finally
            {
                Monitor.Exit(_sendSync);
            }

            return await _completions[messageId].Task;
        }

        public async ValueTask ReplyAsync(Guid messageId, ArraySegment<byte> buffer)
        {
            if (!_options.Duplex)
                throw new NotSupportedException($"Cannot use ReplyAsync with {nameof(_options.Duplex)} option disabled");

            Monitor.Enter(_sendSync);
            try
            {
                _sendPipe.Writer.Write(BitConverter.GetBytes(buffer.Count + sizeof(int) + GUID_LEN));

                _sendPipe.Writer.Write(messageId.ToByteArray());

                Memory<byte> memory = _sendPipe.Writer.GetMemory(buffer.Count);
                buffer.AsMemory().CopyTo(memory);

                _sendPipe.Writer.Advance(buffer.Count);

                await _sendPipe.Writer.FlushAsync();
            }
            finally
            {
                Monitor.Exit(_sendSync);
            }
        }

        public async ValueTask ReplyAsync(Guid messageId, Stream stream)
        {
            if (!_options.Duplex)
                throw new NotSupportedException($"Cannot use ReplyAsync with {nameof(_options.Duplex)} option disabled");

            Monitor.Enter(_sendSync);
            try
            {
                _sendPipe.Writer.Write(BitConverter.GetBytes((int)stream.Length + sizeof(int) + GUID_LEN));

                _sendPipe.Writer.Write(messageId.ToByteArray());

                Memory<byte> memory = _sendPipe.Writer.GetMemory((int)stream.Length);
                int bytesRead = await stream.ReadAsync(memory);
                if (bytesRead != 0)
                {
                    _sendPipe.Writer.Advance(bytesRead);
                    _ = await _sendPipe.Writer.FlushAsync();
                }
            }
            finally
            {
                Monitor.Exit(_sendSync);
            }
        }

        internal async Task ProcessConnection(Socket socket, NetXConnectionOptions options, CancellationToken cancellationToken = default)
        {
            _socket = socket;
            _options = options;
            _token = cancellationToken;
            _recvBuffer = _bufferPool.Rent(_options.RecvBufferSize);
            try
            {
                _sendBuffer = _bufferPool.Rent(_options.SendBufferSize);
                try
                {
                    var writing = FillPipeAsync();
                    var reading = ReadPipeAsync();
                    var sending = SendPipeAsync();

                    await Task.WhenAll(writing, reading);

                    Disconnect();

                    await _sendPipe.Writer.CompleteAsync();
                    await sending;
                }
                finally
                {
                    _bufferPool.Return(_sendBuffer);
                }
            }
            finally
            {
                _bufferPool.Return(_recvBuffer);
            }
        }

        public void Disconnect()
        {
            _socket.Shutdown(SocketShutdown.Both);
            _socket.Close();
        }

        private async Task FillPipeAsync()
        {
            const int minimumBufferSize = 126;

            try
            {
                while (!_token.IsCancellationRequested)
                {
                    // Allocate at least 512 bytes from the PipeWriter.
                    Memory<byte> memory = _receivePipe.Writer.GetMemory(minimumBufferSize);

                    try
                    {
                        int bytesRead = await _socket.ReceiveAsync(memory, SocketFlags.None, _token);
                        if (bytesRead == 0)
                        {
                            _socket.Close(); // Socket is disconnected gracefully, ensure closing.
                            break;
                        }

                        // Tell the PipeWriter how much was read from the Socket.
                        _receivePipe.Writer.Advance(bytesRead);
                    }
                    catch (Exception ex)
                    {
                        if (ex is SocketException || ex is OperationCanceledException)
                        {
                            // Happens when socket is force disconnected, ignoring
                            break;
                        }

                        throw;
                    }

                    // Make the data available to the PipeReader.
                    FlushResult result = await _receivePipe.Writer.FlushAsync();

                    if (result.IsCanceled || result.IsCompleted)
                    {
                        break;
                    }
                }
            }
            finally
            {
                await _receivePipe.Writer.CompleteAsync();
            }
        }

        private async Task ReadPipeAsync()
        {
            try
            {
                while (!_token.IsCancellationRequested)
                {
                    ReadResult result = await _receivePipe.Reader.ReadAsync();
                    ReadOnlySequence<byte> buffer = result.Buffer;

                    if (result.IsCanceled || result.IsCompleted)
                        break;

                    if (buffer.Length > _recvBuffer.Length)
                        throw new Exception($"Recv Buffer is too small. RecvBuffLen = {_recvBuffer.Length} ReceivedLen = {buffer.Length}");

                    while (true)
                    {
                        if (!TryGetRecvMessage(ref buffer))
                            break;
                    }

                    _receivePipe.Reader.AdvanceTo(buffer.Start, buffer.End);
                }
            }
            finally
            {
                await _receivePipe.Reader.CompleteAsync();
            }
        }

        private bool TryGetRecvMessage(ref ReadOnlySequence<byte> buffer)
        {
            if (buffer.IsEmpty || (_options.Duplex && buffer.Length < GUID_LEN))
                return false;

            buffer.CopyTo(_recvBuffer);

            const int COMPLETION_OFFSET = GUID_LEN + sizeof(int);
            var offset = _options.Duplex ? COMPLETION_OFFSET : 0;

            var receivedBuffer = new ArraySegment<byte>(_recvBuffer, 0, (int)buffer.Length);
            var size = _options.Duplex ? BitConverter.ToInt32(_recvBuffer) : GetReceiveMessageSize(in receivedBuffer);

            if (size <= 0 || size > buffer.Length)
                return false;
            
            var messageId = _options.Duplex ? new Guid(new Span<byte>(_recvBuffer, sizeof(int), GUID_LEN)) : Guid.Empty;
            var messageBuffer = new ArraySegment<byte>(_recvBuffer, offset, size - offset);

            ProcessReceivedBuffer(in messageBuffer);

            if (_options.Duplex && _completions.Remove(messageId, out var completion))
            {
                completion.SetResult(messageBuffer);
            }
            else
            {
                var message = new NetXMessage(messageId, messageBuffer);
                OnReceivedMessage(in message);
            }

            var next = buffer.GetPosition(size);
            buffer = buffer.Slice(next);

            return true;
        }

        private async Task SendPipeAsync()
        {
            try
            {
                while (!_token.IsCancellationRequested)
                {
                    ReadResult result = await _sendPipe.Reader.ReadAsync();
                    ReadOnlySequence<byte> buffer = result.Buffer;

                    if (result.IsCanceled || result.IsCompleted)
                        break;

                    if (buffer.Length > _sendBuffer.Length)
                        throw new Exception($"Send Buffer is too small. SendBuffLen = {_sendBuffer.Length} SendLen = {buffer.Length}");

                    while (TryGetSendMessage(ref buffer, out ArraySegment<byte> sendBuff))
                    {
                        try
                        {
                            await _socket.SendAsync(sendBuff, SocketFlags.None, _token);
                        }
                        catch (Exception ex)
                        {
                            if (ex is SocketException || ex is OperationCanceledException)
                            {
                                return;
                            }

                            throw;
                        }
                    }

                    _sendPipe.Reader.AdvanceTo(buffer.Start, buffer.End);
                }
            }
            finally
            {
                await _sendPipe.Reader.CompleteAsync();
            }
        }

        private bool TryGetSendMessage(ref ReadOnlySequence<byte> buffer, out ArraySegment<byte> sendBuff)
        {
            sendBuff = default;

            var offset = _options.Duplex ? 0 : sizeof(int);

            if (buffer.IsEmpty || buffer.Length < sizeof(int))
                return false;

            buffer.CopyTo(_sendBuffer);

            var size = BitConverter.ToInt32(_sendBuffer);
            sendBuff = new ArraySegment<byte>(_sendBuffer, offset, (int)buffer.Length - offset);

            ProcessSendBuffer(in sendBuff);

            var next = buffer.GetPosition(size);
            buffer = buffer.Slice(next);

            return true;
        }

        protected virtual int GetReceiveMessageSize(in ArraySegment<byte> buffer)
        {
            return 0;
        }

        protected virtual void ProcessReceivedBuffer(in ArraySegment<byte> buffer)
        {
        }

        protected virtual void ProcessSendBuffer(in ArraySegment<byte> buffer)
        {
        }

        protected abstract void OnReceivedMessage(in NetXMessage message);
    }
}
