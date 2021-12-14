using NetX;
using System;
using System.Net;
using Microsoft.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using AutoSerializer.Definitions;
using Microsoft.Extensions.Logging;
using NetX.AutoServiceGenerator.Definitions;
{6}

namespace {0};

public class {1}Processor : INetXClientProcessor
{{
    private delegate ValueTask InternalProxy(INetXClientSession client, NetXMessage message, int offset);
    public delegate Task ConnectDelegate();
    public delegate Task DisconnectDelegate();

    private readonly Dictionary<string, Dictionary<ushort, InternalProxy>> _serviceProxies;
    
    private readonly {1} _autoServiceManager;
    private readonly RecyclableMemoryStreamManager _streamManager;
    private readonly ConnectDelegate _connectDelegate;
    private readonly DisconnectDelegate _disconnectDelegate;
    private readonly ILogger _logger;
    private readonly string _identity;

    public {1}Processor({1} autoServiceManager, RecyclableMemoryStreamManager streamManager, ILogger logger, string identity, ConnectDelegate connectDelegate, DisconnectDelegate disconnectDelegate)
    {{
        _autoServiceManager = autoServiceManager;
        _streamManager = streamManager;
        _logger = logger;
        _identity = identity;
        _serviceProxies = new Dictionary<string, Dictionary<ushort, InternalProxy>>();
        _connectDelegate = connectDelegate;
        _disconnectDelegate = disconnectDelegate;
        LoadProxys();
        InitializeServices();
    }}
    private void LoadProxys()
    {{
{4}
    }}

    private void InitializeServices()
    {{
{3}
    }}

    public Task OnConnectedAsync(INetXClientSession client)
    {{
        return _connectDelegate();
    }}

    public Task OnDisconnectedAsync()
    {{
        return _disconnectDelegate();
    }}

    public Task OnReceivedMessageAsync(INetXClientSession client, NetXMessage message)
    {{
        var buffer = message.Buffer;
        var offset = buffer.Offset;
        
        buffer.Read(ref offset, out string interfaceCode);
        buffer.Read(ref offset, out ushort methodCode);

        Task.Run(async () =>
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

            await _serviceProxies[interfaceCode][methodCode](client, message, offset);
        }});
        return Task.CompletedTask;
    }}

    public int GetReceiveMessageSize(INetXClientSession client, in ArraySegment<byte> buffer)
    {{
        throw new NotImplementedException();
    }}

    public void ProcessReceivedBuffer(INetXClientSession client, in ArraySegment<byte> buffer)
    {{
        
    }}

    public void ProcessSendBuffer(INetXClientSession client, in ArraySegment<byte> buffer)
    {{
        
    }}
    
    #region ServiceProviders

{2}
    
    #endregion

    #region ServiceProxys

{5}

    #endregion
}}