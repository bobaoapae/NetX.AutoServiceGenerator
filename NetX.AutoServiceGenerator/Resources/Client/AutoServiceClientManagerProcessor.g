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

    private readonly Dictionary<string, Dictionary<ushort, InternalProxy>> _serviceProxies;
    
    private {1} _autoServiceManager;
    private RecyclableMemoryStreamManager _streamManager;

    public {1}Processor({1} autoServiceManager, RecyclableMemoryStreamManager streamManager)
    {{
        _autoServiceManager = autoServiceManager;
        _streamManager = streamManager;
        _serviceProxies = new Dictionary<string, Dictionary<ushort, InternalProxy>>();
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
        return Task.CompletedTask;
    }}

    public Task OnDisconnectedAsync()
    {{
        return Task.CompletedTask;
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