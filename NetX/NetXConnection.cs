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
        protected readonly Socket _socket;
        protected readonly NetXConnectionOptions _options;

        private readonly Pipe _sendPipe;
        private readonly Pipe _receivePipe;
        private readonly ConcurrentDictionary<Guid, TaskCompletionSource<ArraySegment<byte>>> _completions;

        private readonly byte[] _recvBuffer;
        private readonly byte[] _sendBuffer;

        private readonly object _sendSync = new();

        const int GUID_LEN = 16;
        private static readonly byte[] _emptyGuid = Guid.Empty.ToByteArray();
        private static readonly ArrayPool<byte> _bufferPool = ArrayPool<byte>.Shared;

        public NetXConnection(Socket socket, NetXConnectionOptions options)
        {
            _socket = socket;
            _options = options;

            _sendPipe = new Pipe();
            _receivePipe = new Pipe();
            _completions = new ConcurrentDictionary<Guid, TaskCompletionSource<ArraySegment<byte>>>();

            _recvBuffer = _bufferPool.Rent(_options.RecvBufferSize);
            _sendBuffer = _bufferPool.Rent(_options.SendBufferSize);

            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, _options.RecvBufferSize);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, _options.SendBufferSize);
        }

        #region Send Methods
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

            return await WaitForRequestAsync(_completions[messageId]);
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
                stream.Position = 0;

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

            return await WaitForRequestAsync(_completions[messageId]);
        }

        private async ValueTask<ArraySegment<byte>> WaitForRequestAsync(TaskCompletionSource<ArraySegment<byte>> source)
        {
            var delayTask = Task.Delay(_options.DuplexTimeout)
                .ContinueWith((_) => source.TrySetException(new TimeoutException()));

            await Task.WhenAny(delayTask, source.Task);

            return source.Task.Result;
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
                stream.Position = 0;

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
        #endregion

        internal async Task ProcessConnection(CancellationToken cancellationToken = default)
        {
            var writing = FillPipeAsync(cancellationToken);
            var reading = ReadPipeAsync(cancellationToken);
            var sending = SendPipeAsync(cancellationToken);

            await Task.WhenAll(writing, reading, sending);
        }

        public void Disconnect()
        {
            _receivePipe.Reader.CancelPendingRead();
            _sendPipe.Reader.CancelPendingRead();

            if (_socket.Connected)
            {
                _socket.Shutdown(SocketShutdown.Both);
                _socket.Disconnect(true);
            }
        }

        private async Task FillPipeAsync(CancellationToken cancellationToken)
        {
            const int minimumBufferSize = 126;

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    // Allocate at least 512 bytes from the PipeWriter.
                    Memory<byte> memory = _receivePipe.Writer.GetMemory(minimumBufferSize);

                    int bytesRead = await _socket.ReceiveAsync(memory, SocketFlags.None, cancellationToken);
                    if (bytesRead == 0)
                    {
                        break;
                    }

                    // Tell the PipeWriter how much was read from the Socket.
                    _receivePipe.Writer.Advance(bytesRead);

                    // Make the data available to the PipeReader.
                    FlushResult result = await _receivePipe.Writer.FlushAsync();

                    if (result.IsCanceled || result.IsCompleted)
                    {
                        break;
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (SocketException) { }
            finally
            {
                await _receivePipe.Writer.CompleteAsync();
                Disconnect();
            }
        }

        private async Task ReadPipeAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    ReadResult result = await _receivePipe.Reader.ReadAsync();
                    ReadOnlySequence<byte> buffer = result.Buffer;

                    if (result.IsCanceled || result.IsCompleted)
                        break;

                    if (buffer.Length > _recvBuffer.Length)
                        throw new Exception($"Recv Buffer is too small. RecvBuffLen = {_recvBuffer.Length} ReceivedLen = {buffer.Length}");

                    while (TryGetRecvMessage(ref buffer, out var message))
                    {
                        await OnReceivedMessageAsync(message);
                    }

                    _receivePipe.Reader.AdvanceTo(buffer.Start, buffer.End);
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                await _receivePipe.Reader.CompleteAsync();
                Disconnect();
            }
        }

        private bool TryGetRecvMessage(ref ReadOnlySequence<byte> buffer, out NetXMessage message)
        {
            message = default;

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

            var next = buffer.GetPosition(size);
            buffer = buffer.Slice(next);

            if (_options.Duplex && _completions.Remove(messageId, out var completion))
            {
                completion.TrySetResult(messageBuffer);
                return false;
            }

            message = new NetXMessage(messageId, _options.CopyBuffer ? messageBuffer.ToArray() : messageBuffer);

            return true;
        }

        private async Task SendPipeAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    ReadResult result = await _sendPipe.Reader.ReadAsync();
                    ReadOnlySequence<byte> buffer = result.Buffer;

                    if (result.IsCanceled || result.IsCompleted)
                        break;

                    if (buffer.Length > _sendBuffer.Length)
                        throw new Exception($"Send Buffer is too small. SendBuffLen = {_sendBuffer.Length} SendLen = {buffer.Length}");

                    while (TryGetSendMessage(ref buffer, out ArraySegment<byte> sendBuff))
                    {
                        if (_socket.Connected)
                        {
                            await _socket.SendAsync(sendBuff, SocketFlags.None, cancellationToken);
                        }
                    }

                    _sendPipe.Reader.AdvanceTo(buffer.Start, buffer.End);
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                await _sendPipe.Reader.CompleteAsync();
                Disconnect();
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

        protected abstract Task OnReceivedMessageAsync(NetXMessage message);
    }
}
