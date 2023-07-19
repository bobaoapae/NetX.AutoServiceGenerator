using NetX;
using System;
using System.Net;
using Microsoft.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using AutoSerializer.Definitions;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using NetX.AutoServiceGenerator.Definitions;
{6}

namespace {0};

public class {1}Processor : INetXClientProcessor
{{
    private delegate ValueTask InternalProxy(INetXClientSession client, NetXMessage message, int offset);
    public delegate ValueTask ConnectDelegate();
    public delegate ValueTask DisconnectDelegate();

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

    public ValueTask OnConnectedAsync(INetXClientSession client, CancellationToken cancellationToken)
    {{
        return _connectDelegate();
    }}

    public ValueTask OnDisconnectedAsync()
    {{
        return _disconnectDelegate();
    }}

    public ValueTask OnReceivedMessageAsync(INetXClientSession client, NetXMessage message, CancellationToken cancellationToken)
    {{
        if(!MemoryMarshal.TryGetArray(message.Buffer, out var buffer))
            return ValueTask.CompletedTask;

        var offset = buffer.Offset;
        
        buffer.Read(ref offset, out int len_interfaceCode);
        buffer.Read(ref offset, in len_interfaceCode, out string interfaceCode);
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
        }}, cancellationToken);
        return ValueTask.CompletedTask;
    }}

    public int GetReceiveMessageSize(INetXClientSession client, in ReadOnlyMemory<byte> buffer)
    {{
        throw new NotImplementedException();
    }}

    public void ProcessReceivedBuffer(INetXClientSession client, in ReadOnlyMemory<byte> buffer)
    {{
        
    }}

    public void ProcessSendBuffer(INetXClientSession client, in ReadOnlyMemory<byte> buffer)
    {{
        
    }}

    #region Authentication
    public async Task<{7}> SendAuthentication(NetX.INetXClient client, IAutoSerialize ipsAuthenticationProto, System.Threading.CancellationToken cancellationToken)
    {{
        try
        {{
            await using var stream = _streamManager.GetStream("ipcInternalAuthentication", 4096, true);
            stream.ExWrite("InternalIpcAuthentication".Length);
            stream.ExWrite("InternalIpcAuthentication");
            stream.ExWrite(Convert.ToUInt16(1456));
            stream.ExWrite(ipsAuthenticationProto);
            
            var bufferResult = await client.RequestAsync(stream, cancellationToken);
            var offset = bufferResult.Offset;

            bufferResult.Read(ref offset, out {7} authResult);    
            return authResult;
        }}
        catch(Exception ex)
        {{
            _logger?.LogError(ex, "{{identity}}: Error while sending authentication", _identity);
            return new {7}();
        }}
    }}
    #endregion
    
    #region ServiceProviders

{2}
    
    #endregion

    #region ServiceProxys

{5}

    #endregion
}}