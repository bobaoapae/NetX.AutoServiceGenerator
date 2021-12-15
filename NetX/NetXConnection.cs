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
using Microsoft.Extensions.Logging;

namespace NetX
{
    public abstract class NetXConnection : INetXConnection
    {
        protected readonly Socket _socket;
        protected readonly NetXConnectionOptions _options;

        protected readonly string _appName;
        protected readonly ILogger _logger;

        private readonly Pipe _sendPipe;
        private readonly Pipe _receivePipe;
        private readonly ConcurrentDictionary<Guid, TaskCompletionSource<ArraySegment<byte>>> _completions;

        private byte[] _recvBuffer;
        private byte[] _sendBuffer;
        private CancellationTokenSource _cancellationTokenSource;

        private readonly bool _reuseSocket;

        private readonly object _sync = new();

        const int GUID_LEN = 16;
        private static readonly byte[] _emptyGuid = Guid.Empty.ToByteArray();
        private static readonly ArrayPool<byte> _bufferPool = ArrayPool<byte>.Shared;

        public NetXConnection(Socket socket, NetXConnectionOptions options, string name, ILogger logger, bool reuseSocket = false)
        {
            _socket = socket;
            _options = options;

            _appName = name;
            _logger = logger;

            _sendPipe = new Pipe();
            _receivePipe = new Pipe();
            _completions = new ConcurrentDictionary<Guid, TaskCompletionSource<ArraySegment<byte>>>();

            _reuseSocket = reuseSocket;

            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, _options.RecvBufferSize);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, _options.SendBufferSize);
        }

        #region Send Methods
        public async ValueTask SendAsync(ArraySegment<byte> buffer)
        {
            Monitor.Enter(_sync);
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
                Monitor.Exit(_sync);
            }
        }

        public async ValueTask SendAsync(Stream stream)
        {
            Monitor.Enter(_sync);
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
                Monitor.Exit(_sync);
            }
        }

        public async ValueTask<ArraySegment<byte>> RequestAsync(ArraySegment<byte> buffer)
        {
            if (!_options.Duplex)
                throw new NotSupportedException($"Cannot use RequestAsync with {nameof(_options.Duplex)} option disabled");

            var messageId = Guid.NewGuid();
            if (!_completions.TryAdd(messageId, new TaskCompletionSource<ArraySegment<byte>>()))
                throw new Exception($"Cannot track completion for MessageId = {messageId}");

            Monitor.Enter(_sync);
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
                Monitor.Exit(_sync);
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

            Monitor.Enter(_sync);
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
                Monitor.Exit(_sync);
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

            Monitor.Enter(_sync);
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
                Monitor.Exit(_sync);
            }
        }

        public async ValueTask ReplyAsync(Guid messageId, Stream stream)
        {
            if (!_options.Duplex)
                throw new NotSupportedException($"Cannot use ReplyAsync with {nameof(_options.Duplex)} option disabled");

            Monitor.Enter(_sync);
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
                Monitor.Exit(_sync);
            }
        }
        #endregion

        internal async Task ProcessConnection(CancellationToken cancellationToken = default)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            cancellationToken.Register(() => _cancellationTokenSource.Cancel());

            _recvBuffer = _bufferPool.Rent(_options.RecvBufferSize + sizeof(int));
            try
            {
                _sendBuffer = _bufferPool.Rent(_options.SendBufferSize + sizeof(int));
                try
                {
                    var writing = FillPipeAsync(_cancellationTokenSource.Token);
                    var reading = ReadPipeAsync(_cancellationTokenSource.Token);
                    var sending = SendPipeAsync(_cancellationTokenSource.Token);

                    await Task.WhenAll(writing, reading, sending);
                }
                finally
                {
                    _bufferPool.Return(_sendBuffer, true);
                }
            }
            finally
            {
                _bufferPool.Return(_recvBuffer, true);
            }
        }

        public void Disconnect()
        {
            _cancellationTokenSource.Cancel();
            if (_socket.Connected)
            {
                _socket.Shutdown(SocketShutdown.Both);
                _socket.Disconnect(_reuseSocket);
            }
        }

        private async Task FillPipeAsync(CancellationToken cancellationToken)
        {
            const int minimumBufferSize = 512;

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    // Allocate at least 512 bytes from the PipeWriter.
                    Memory<byte> memory = _receivePipe.Writer.GetMemory(minimumBufferSize);

                    int bytesRead = await _socket.ReceiveAsync(memory, SocketFlags.None, cancellationToken);
                    if (bytesRead == 0)
                    {
                        if (!_reuseSocket)
                        {
                            _socket.Close();
                        }

                        break;
                    }

                    // Tell the PipeWriter how much was read from the Socket.
                    _receivePipe.Writer.Advance(bytesRead);

                    // Make the data available to the PipeReader.
                    FlushResult result = await _receivePipe.Writer.FlushAsync(cancellationToken);

                    if (result.IsCanceled || result.IsCompleted)
                    {
                        break;
                    }
                }
            }
            catch (SocketException) { }
            catch (OperationCanceledException) { }
            finally
            {
                await _receivePipe.Writer.CompleteAsync();
                _receivePipe.Reader.CancelPendingRead();
                _sendPipe.Reader.CancelPendingRead();
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

                    while (TryGetRecvMessage(ref buffer, out var message))
                    {
                        if (message.HasValue)
                        {
                            await OnReceivedMessageAsync(message.Value);
                        }
                    }

                    _receivePipe.Reader.AdvanceTo(buffer.Start, buffer.End);
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                Disconnect();
                await _receivePipe.Reader.CompleteAsync();
            }
        }

        private bool TryGetRecvMessage(ref ReadOnlySequence<byte> buffer, out NetXMessage? message)
        {
            message = null;

            const int DUPLEX_HEADER_SIZE = sizeof(int) + GUID_LEN;

            if (buffer.IsEmpty || (_options.Duplex && buffer.Length < DUPLEX_HEADER_SIZE))
            {
                return false;
            }

            var headerOffset = _options.Duplex ? DUPLEX_HEADER_SIZE : 0;

            var minRecvSize = Math.Min(_options.RecvBufferSize, buffer.Length);
            buffer.Slice(0, _options.Duplex ? headerOffset : minRecvSize).CopyTo(_recvBuffer);
            
            var size = _options.Duplex ? BitConverter.ToInt32(_recvBuffer) : GetReceiveMessageSize(new ArraySegment<byte>(_recvBuffer, 0, (int)minRecvSize));
            var messageId = _options.Duplex ? new Guid(new Span<byte>(_recvBuffer, sizeof(int), GUID_LEN)) : Guid.Empty;

            if (size > _options.RecvBufferSize)
                throw new Exception($"Recv Buffer is too small. RecvBuffLen = {_options.RecvBufferSize} ReceivedLen = {size}");

            if (size > buffer.Length)
                return false;

            buffer.Slice(headerOffset, size - headerOffset).CopyTo(_recvBuffer);
            
            var messageBuffer = new ArraySegment<byte>(_recvBuffer, 0, size - headerOffset);
            ProcessReceivedBuffer(in messageBuffer);

            var next = buffer.GetPosition(size);
            buffer = buffer.Slice(next);

            if (_options.Duplex && _completions.Remove(messageId, out var completion))
            {
                return completion.TrySetResult(messageBuffer);
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
            catch (SocketException) { }
            catch (OperationCanceledException) { }
            finally
            {
                Disconnect();
                await _sendPipe.Reader.CompleteAsync();
            }
        }

        private bool TryGetSendMessage(ref ReadOnlySequence<byte> buffer, out ArraySegment<byte> sendBuff)
        {
            sendBuff = default;

            var offset = _options.Duplex ? 0 : sizeof(int);

            if (buffer.IsEmpty || buffer.Length < sizeof(int))
                return false;

            buffer.Slice(0, sizeof(int)).CopyTo(_sendBuffer);
            var size = BitConverter.ToInt32(_sendBuffer);

            if (size > _options.SendBufferSize)
                throw new Exception($"Send Buffer is too small. SendBuffLen = {_options.SendBufferSize} SendLen = {size}");

            if (size > buffer.Length)
                return false;

            buffer.Slice(offset, size).CopyTo(_sendBuffer);

            sendBuff = new ArraySegment<byte>(_sendBuffer, 0, size);

            ProcessSendBuffer(in sendBuff);

            var next = buffer.GetPosition(size + offset);
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
