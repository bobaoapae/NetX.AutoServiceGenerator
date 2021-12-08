using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;
using NetX.Options;

namespace NetX
{
    public class NetXClient : NetXConnection, INetXClient
    {
        private new readonly NetXClientOptions _options;

        internal NetXClient(NetXClientOptions options)
        {
            _options = options;
        }

        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = _options.NoDelay
            };

            await socket.ConnectAsync(_options.EndPoint, cancellationToken);

            _ = ProcessClientConnection(socket, cancellationToken);
        }

        private async Task ProcessClientConnection(Socket socket, CancellationToken cancellationToken)
        {
            try
            {
                await ProcessConnection(socket, _options, cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        protected override int GetReceiveMessageSize(in ArraySegment<byte> buffer)
            => _options.Processor.GetReceiveMessageSize(this, in buffer);

        protected override void OnReceivedMessage(in NetXMessage message)
            => _options.Processor.OnReceivedMessage(this, in message);

        protected override void ProcessReceivedBuffer(in ArraySegment<byte> buffer)
            => _options.Processor.ProcessReceivedBuffer(this, in buffer);

        protected override void ProcessSendBuffer(in ArraySegment<byte> buffer)
            => _options.Processor.ProcessSendBuffer(this, in buffer);
    }
}
