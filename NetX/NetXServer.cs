using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NetX.Options;

namespace NetX
{
    public class NetXServer : INetXServer
    {
        private Socket _socket;

        private readonly NetXServerOptions _options;

        internal NetXServer(NetXServerOptions options)
        {
            _options = options;
        }

        public void Listen(CancellationToken cancellationToken = default)
        {
            _socket = new Socket(SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = _options.NoDelay
            };

            _socket.Bind(_options.EndPoint);
            _socket.Listen();

            _ = StartAcceptAsync(cancellationToken);
        }

        private async Task StartAcceptAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var sessionSocket = await _socket.AcceptAsync(cancellationToken);
                    sessionSocket.NoDelay = _options.NoDelay;

                    _ = ProcessSessionConnection(sessionSocket, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private async Task ProcessSessionConnection(Socket sessionSocket, CancellationToken cancellationToken)
        {
            try
            {
                var session = (NetXSession)_options.Processor.CreateSession();
                await session.ProcessConnection(sessionSocket, _options, cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
