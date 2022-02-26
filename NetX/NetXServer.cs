using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

            _ = Task.Factory.StartNew(() =>
            {
                _ = StartAcceptAsync(cancellationToken);
            }, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private async Task StartAcceptAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var sessionSocket = await _socket.AcceptAsync(cancellationToken);
                        _ = Task.Factory.StartNew(() =>
                        {
                            _ = ProcessSessionConnection(sessionSocket, cancellationToken);
                        }, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
                    }
                    catch (SocketException socketException)
                    {
                        _logger?.LogError(socketException, "{svrName}: An exception was throw on accept new connections", _serverName);
                    }
                }

                _logger?.LogInformation("{svrName}: Shutdown", _serverName);

                _socket.Shutdown(SocketShutdown.Both);
                _socket.Disconnect(true);

                foreach (var session in _sessions.Values)
                    session.Disconnect();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger?.LogCritical(ex, "{svrName}: An exception was throw on listen pipe", _serverName);
            }
        }

        private async Task ProcessSessionConnection(Socket sessionSocket, CancellationToken cancellationToken)
        {
            var remoteAddress = ((IPEndPoint)sessionSocket.RemoteEndPoint).Address.MapToIPv4();
            if (_options.UseProxy)
            {
                await using var stream = new NetworkStream(sessionSocket);
                var proxyprotocol = new ProxyProtocol(stream, sessionSocket.RemoteEndPoint as IPEndPoint);
                var realRemoteEndpoint = await proxyprotocol.GetRemoteEndpoint();
                remoteAddress = realRemoteEndpoint.Address.MapToIPv4();
            }

            var session = new NetXSession(sessionSocket, remoteAddress, _options, _serverName, _logger);
            try
            {
                if (_sessions.TryAdd(session.Id, session))
                {
                    await _options.Processor.OnSessionConnectAsync(session);
                    await session.ProcessConnection(cancellationToken);
                }
                else
                {
                    _logger?.LogCritical("{svrName}: Fail on add Session {sessId} to sessions dictionary", _serverName, session.Id);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "{svrName}: An exception was throw on Session {sessId}", _serverName, session.Id);
            }
            finally
            {
                if (_sessions.Remove(session.Id, out var netXSession))
                {
                    try
                    {
                        netXSession.Disconnect();
                    }
                    catch (Exception)
                    {
                        //ignore
                    }
                    await _options.Processor.OnSessionDisconnectAsync(session.Id);
                }
            }
        }
    }
}
