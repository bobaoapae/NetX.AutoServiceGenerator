using NetX;
using System;
using System.Net;
using Microsoft.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using NetX.AutoServiceGenerator.Definitions;
{6}

namespace {0};

public class {1}Processor : INetXServerProcessor
{{
    public delegate bool TryGetCallingSession(out {1}Session session);
    public delegate bool TryGetSession(Guid guid, out {1}Session session);
    private delegate ValueTask InternalProxy({1}Session session, NetXMessage message, int offset);

    private readonly ConcurrentDictionary<Guid, {1}Session> _sessions;
    private readonly Dictionary<string, Dictionary<ushort, InternalProxy>> _serviceProxies;

    private AsyncLocal<{1}Session> _currentSession;

    private {1} _autoServerManager;
    private RecyclableMemoryStreamManager _streamManager;

    public {1}Processor({1} autoServerManager, RecyclableMemoryStreamManager streamManager)
    {{
        _currentSession = new AsyncLocal<{1}Session>();
        _autoServerManager = autoServerManager;
        _streamManager = streamManager;
        _serviceProxies = new Dictionary<string, Dictionary<ushort, InternalProxy>>();
        _sessions = new ConcurrentDictionary<Guid, {1}Session>();
        InitializeServices();
        LoadProxys();
    }}

    private void InitializeServices()
    {{
{3}
    }}

    private void LoadProxys()
    {{
{4}
    }}
    
    
    private bool TryGetCallingSessionProxy(out {1}Session session)
    {{
        if (_currentSession.Value != null)
        {{
            session = _currentSession.Value;
            return true;
        }}

        session = null;

        return false;
    }}

    public Task OnSessionConnectAsync(INetXSession session)
    {{
        if(!_sessions.TryAdd(session.Id, new {1}Session(session, _streamManager)))
            session.Disconnect();
        return Task.CompletedTask;
    }}

    public Task OnSessionDisconnectAsync(Guid sessionId)
    {{
        _sessions.TryRemove(sessionId, out _);
        return Task.CompletedTask;
    }}

    public Task OnReceivedMessageAsync(INetXSession session, NetXMessage message)
    {{
        var buffer = message.Buffer;
        var offset = buffer.Offset;
        
        buffer.Read(ref offset, out string interfaceCode);
        buffer.Read(ref offset, out ushort methodCode);

        Task.Run(async () =>
        {{
            if(!_sessions.TryGetValue(session.Id, out var autoServiceSession))
                session.Disconnect();
            await _serviceProxies[interfaceCode][methodCode](autoServiceSession, message, offset);
        }});
        
        return Task.CompletedTask;
    }}

    public int GetReceiveMessageSize(INetXSession session, in ArraySegment<byte> buffer)
    {{
        throw new NotImplementedException();
    }}

    public void ProcessReceivedBuffer(INetXSession session, in ArraySegment<byte> buffer)
    {{
        
    }}

    public void ProcessSendBuffer(INetXSession session, in ArraySegment<byte> buffer)
    {{
        
    }}
    
    #region ServiceProviders

{2}
    
    #endregion

    #region ServiceProxys

{5}

    #endregion
}}