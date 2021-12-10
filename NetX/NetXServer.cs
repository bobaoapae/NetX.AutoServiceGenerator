using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NetX.Options;

namespace NetX
{
    public class NetXServer : INetXServer
    {
        private readonly ILogger _logger;
        private readonly string _serverName;
        private readonly Socket _socket;
        private readonly NetXServerOptions _options;
        private readonly ConcurrentDictionary<Guid, INetXSession> _sessions;
        private readonly List<Task> _sessionTasks;

        internal NetXServer(NetXServerOptions options, ILoggerFactory loggerFactory = null, string serverName = null)
        {
            _logger = loggerFactory?.CreateLogger<NetXServer>();
            _serverName = serverName ?? nameof(NetXServer);

            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = options.NoDelay,
                LingerState = new LingerOption(true, 5)
            };

            _socket.Bind(options.EndPoint);

            _options = options;
            _sessions = new ConcurrentDictionary<Guid, INetXSession>();
            _sessionTasks = new List<Task>();
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
            _socket.Listen(_options.Backlog);

            _logger?.LogInformation("{svrName}: Tcp server listening on {ip}:{port}", _serverName, _options.EndPoint.Address, _options.EndPoint.Port);

            _ = StartAcceptAsync(cancellationToken);
        }

        private async Task StartAcceptAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var sessionSocket = await _socket.AcceptAsync(cancellationToken);

                    var sessionTask = ProcessSessionConnection(sessionSocket, cancellationToken)
                        .ContinueWith((task) => _sessionTasks.Remove(task));

                    _sessionTasks.Add(sessionTask);
                }

                _logger?.LogInformation("{svrName}: Shutdown", _serverName);

                _socket.Shutdown(SocketShutdown.Both);
                _socket.Disconnect(true);

                foreach (var session in _sessions.Values)
                    session.Disconnect();

                await Task.WhenAll(_sessionTasks.ToArray());
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger?.LogCritical(ex, "{svrName}: An exception was throwed on listen pipe", _serverName);
            }
        }

        private async Task ProcessSessionConnection(Socket sessionSocket, CancellationToken cancellationToken)
        {
            var remoteAddress = ((IPEndPoint)sessionSocket.RemoteEndPoint).Address.MapToIPv4();
            if (_options.UseProxy)
            {
                using var stream = new NetworkStream(sessionSocket);
                var proxyprotocol = new ProxyProtocol(stream, sessionSocket.RemoteEndPoint as IPEndPoint);
                var realRemoteEndpoint = await proxyprotocol.GetRemoteEndpoint();
                remoteAddress = realRemoteEndpoint.Address.MapToIPv4();
            }

            var session = new NetXSession(sessionSocket, remoteAddress, _options);
            try
            {
                if (_sessions.TryAdd(session.Id, session))
                {
                    await _options.Processor.OnSessionConnectAsync(session);
                    await session.ProcessConnection(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "{svrName}: An exception was throwed on Session {sessId}", _serverName, session.Id);
            }
            finally
            {
                _sessions.Remove(session.Id, out _);
                await _options.Processor.OnSessionDisconnectAsync(session.Id);
            }

            sessionSocket.Close(1);
        }
    }
}
