using NetX;
using System.Threading.Tasks;
using Microsoft.IO;
using NetX.AutoServiceGenerator.Definitions;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using AutoServiceClientSample.Definitions;
using AutoServiceServerSample.Definitions;

namespace AutoServiceClientSample;

public class AutoServiceClientManagerProcessor : INetXClientProcessor
{
    private delegate ValueTask InternalProxy(INetXClientSession client, NetXMessage message, int offset);

    private readonly Dictionary<ushort, Dictionary<ushort, InternalProxy>> _serviceProxys;
    
    private AutoServiceClientManager _autoServiceClientManager;
    private RecyclableMemoryStreamManager _streamManager;

    public AutoServiceClientManagerProcessor(AutoServiceClientManager autoAutoServiceClientManager, RecyclableMemoryStreamManager streamManager)
    {
        _autoServiceClientManager = autoAutoServiceClientManager;
        _streamManager = streamManager;
        _serviceProxys = new Dictionary<ushort, Dictionary<ushort, InternalProxy>>();
        LoadProxys();
        InitializeServices();
    }
    private void LoadProxys()
    {
        if(!_serviceProxys.ContainsKey(1))
            _serviceProxys.Add(1, new Dictionary<ushort, InternalProxy>());
        
        if(!_serviceProxys[1].ContainsKey(0))
            _serviceProxys[1].Add(0, InternalProxy_AutoServiceClientSample_1_0_ReceiveLink);
    }

    private void InitializeServices()
    {
        _autoServiceClientSample = new AutoServiceClientSample();
    }

    public int GetReceiveMessageSize(INetXClientSession client, in ArraySegment<byte> buffer)
    {
        throw new NotImplementedException();
    }

    public void ProcessReceivedBuffer(INetXClientSession client, in ArraySegment<byte> buffer)
    {
        
    }

    public void OnReceivedMessage(INetXClientSession client, in NetXMessage message)
    {
        var buffer = message.Buffer;
        var offset = buffer.Offset;
        
        buffer.Read(ref offset, out ushort interfaceCode);
        buffer.Read(ref offset, out ushort methodCode);

        var xMessage = message;
        Task.Run(async () =>
        {
            await _serviceProxys[interfaceCode][methodCode](client, xMessage, offset);
        });
    }

    public void ProcessSendBuffer(INetXClientSession client, in ArraySegment<byte> buffer)
    {
        
    }
    
    #region ServiceProviders

    private IAutoServiceClientSample _autoServiceClientSample;
    
    #endregion

    #region ServiceProxys

    private async ValueTask InternalProxy_AutoServiceClientSample_1_0_ReceiveLink(INetXClientSession client, NetXMessage message, int offset)
    {
        var inputBuffer = message.Buffer;
        inputBuffer.Read(ref offset, out ushort value);

        var autoServiceClientSample_1_0_ReceiveLink_Result = await _autoServiceClientSample.ReceiveLink(value);
        
        var stream = (RecyclableMemoryStream)_streamManager.GetStream("AutoServiceClientSample_1_0_ReceiveLink", 4096, true);
        try
        {
            stream.Write(autoServiceClientSample_1_0_ReceiveLink_Result);
            
            await client.ReplyAsync(message.Id, stream);
        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
            await stream.DisposeAsync();
        }
    }

    #endregion
}