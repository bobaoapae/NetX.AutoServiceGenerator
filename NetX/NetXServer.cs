using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
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
        private readonly ConcurrentDictionary<Guid, INetXSession> _sessions;

        internal NetXServer(NetXServerOptions options)
        {
            _options = options;
            _sessions = new ConcurrentDictionary<Guid, INetXSession>();
        }

        public bool TryGetSession(Guid sessionId, out INetXSession session)
        {
            return _sessions.TryGetValue(sessionId, out session);
        }

        public IEnumerable<INetXSession> GetAllSessions()
        {
            return _sessions.Values;
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

                _socket.Shutdown(SocketShutdown.Both);
                _socket.Close();

                foreach (var session in _sessions.Values)
                    session.Disconnect();
            }
            catch (OperationCanceledException)
            {
            }
        }

        private async Task ProcessSessionConnection(Socket sessionSocket, CancellationToken cancellationToken)
        {
            try
            {
                var sessionId = Guid.NewGuid();

                var address = ((IPEndPoint)sessionSocket.RemoteEndPoint).Address.MapToIPv4();
                if (_options.UseProxy)
                {
                    using var stream = new NetworkStream(sessionSocket);
                    var proxyprotocol = new ProxyProtocol(stream, sessionSocket.RemoteEndPoint as IPEndPoint);
                    var realRemoteEndpoint = await proxyprotocol.GetRemoteEndpoint();
                    address = realRemoteEndpoint.Address.MapToIPv4();
                }

                var session = (NetXSession)_options.Processor.CreateSession(sessionId, address);
                if (session == null)
                    throw new Exception("Created session is null");

                if (_sessions.TryAdd(sessionId, session))
                {
                    await session.ProcessConnection(sessionSocket, _options, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
