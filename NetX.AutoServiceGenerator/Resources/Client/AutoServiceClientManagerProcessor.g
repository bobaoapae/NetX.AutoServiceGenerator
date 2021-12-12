using NetX;
using System;
using System.Net;
using Microsoft.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using NetX.AutoServiceGenerator.Definitions;
{6}

namespace {0};

public class {1}Processor : INetXClientProcessor
{{
    private delegate ValueTask InternalProxy(INetXClientSession client, NetXMessage message, int offset);
    public delegate Task ConnectDelegate();
    public delegate Task DisconnectDelegate();

    private readonly Dictionary<string, Dictionary<ushort, InternalProxy>> _serviceProxies;
    
    private {1} _autoServiceManager;
    private RecyclableMemoryStreamManager _streamManager;
    private ConnectDelegate _connectDelegate;
    private DisconnectDelegate _disconnectDelegate;

    public {1}Processor({1} autoServiceManager, RecyclableMemoryStreamManager streamManager, ConnectDelegate connectDelegate, DisconnectDelegate disconnectDelegate)
    {{
        _autoServiceManager = autoServiceManager;
        _streamManager = streamManager;
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