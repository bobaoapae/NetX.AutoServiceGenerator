﻿using NetX;
using System;
using System.Net;
using Microsoft.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using AutoSerializer.Definitions;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using NetX.AutoServiceGenerator.Definitions;
{6}

namespace {0};

public class {1}Processor : INetXServerProcessor
{{
    public delegate bool TryGetCallingSession(out {1}Session session);
    public delegate bool TryGetSession(Guid guid, out {1}Session session);
    private delegate ValueTask InternalProxy({1}Session session, NetXMessage message, int offset);
    public delegate ValueTask ConnectDelegate({1}Session session);
    public delegate ValueTask DisconnectDelegate({1}Session session, DisconnectReason reason);

    private readonly ConcurrentDictionary<Guid, {1}Session> _sessions;
    private readonly Dictionary<string, Dictionary<ushort, InternalProxy>> _serviceProxies;
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource> _pendingAuthentications;

    private AsyncLocal<{1}Session> _currentSession;

    private readonly {1} _autoServerManager;
    private readonly RecyclableMemoryStreamManager _streamManager;
    private readonly ConnectDelegate _connectDelegate;
    private readonly DisconnectDelegate _disconnectDelegate;
    private readonly ILogger _logger;
    private readonly string _identity;

    public {1}Processor({1} autoServerManager, RecyclableMemoryStreamManager streamManager, ILogger logger, string identity, ConnectDelegate connectDelegate, DisconnectDelegate disconnectDelegate)
    {{
        _currentSession = new AsyncLocal<{1}Session>();
        _autoServerManager = autoServerManager;
        _streamManager = streamManager;
        _serviceProxies = new Dictionary<string, Dictionary<ushort, InternalProxy>>();
        _pendingAuthentications = new ConcurrentDictionary<Guid, TaskCompletionSource>();
        _connectDelegate = connectDelegate;
        _disconnectDelegate = disconnectDelegate;
        _logger = logger;
        _identity = identity;
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

    public async ValueTask OnSessionConnectAsync(INetXSession session, CancellationToken cancellationToken)
    {{
        var internalSession = new {1}Session(session, _logger, _streamManager);
        if(!_sessions.TryAdd(session.Id, internalSession))
        {{
            _logger?.LogError("{{identity}}: Fail on add session ({{sessionId}}) to processor session list", _identity, session.Id);
            session.Disconnect();
        }}
        
{7}
            
        await _connectDelegate(internalSession);
    }}

    public ValueTask OnSessionDisconnectAsync(Guid sessionId, DisconnectReason reason)
    {{
        if(!_sessions.TryRemove(sessionId, out var session))
        {{
            _logger?.LogError("{{identity}}: Fail on remove session ({{sessionId}}) from processor session list", _identity, sessionId);
            return ValueTask.CompletedTask;
        }}
        return _disconnectDelegate(({1}Session)session, reason);
    }}

    #pragma warning disable CS1998
    public async ValueTask OnReceivedMessageAsync(INetXSession session, NetXMessage message, CancellationToken cancellationToken)
    #pragma warning restore CS1998
    {{
        if(!MemoryMarshal.TryGetArray(message.Buffer, out var buffer))
        {{
            message.Dispose();
            return;
        }}

        var offset = buffer.Offset;
        
        buffer.Read(ref offset, out int len_interfaceCode);
        buffer.Read(ref offset, in len_interfaceCode, out string interfaceCode);
        buffer.Read(ref offset, out ushort methodCode);

        if(!_sessions.TryGetValue(session.Id, out var autoServiceSession))
        {{
            _logger?.LogError("{{identity}}: Received request but session ({{sessionId}}) was not found on processor session list", _identity, session.Id);
            session.Disconnect();
            message.Dispose();
            return;
        }}

{8}

        _ = Task.Run(async () =>
        {{
            using(message)
            {{
                if(!_serviceProxies.ContainsKey(interfaceCode))
                {{
                    _logger?.LogWarning("{{identity}}: Received request to unregistered service ({{interfaceCode}})", _identity, interfaceCode);
                    return;
                }}

                if(!_serviceProxies[interfaceCode].ContainsKey(methodCode))
                {{
                    _logger?.LogWarning("{{identity}}: Received invalid method ({{methodCode}}) request to service ({{interfaceCode}}) ", _identity, interfaceCode, methodCode);
                    return;
                }}
               
                await _serviceProxies[interfaceCode][methodCode](autoServiceSession, message, offset);   
            }}
        }}, cancellationToken);
    }}

    public int GetReceiveMessageSize(INetXSession session, in ReadOnlyMemory<byte> buffer)
    {{
        throw new NotImplementedException();
    }}

    public void ProcessReceivedBuffer(INetXSession session, in ReadOnlyMemory<byte> buffer)
    {{
        
    }}

    public void ProcessSendBuffer(INetXSession session, in ReadOnlyMemory<byte> buffer)
    {{
        
    }}
    
    #region ServiceProviders

{2}
    
    #endregion

    #region ServiceProxys

{5}

    #endregion
}}