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

    private readonly Dictionary<string, Dictionary<ushort, InternalProxy>> _serviceProxys;
    
    private AutoServiceClientManager _autoServiceClientManager;
    private RecyclableMemoryStreamManager _streamManager;

    public AutoServiceClientManagerProcessor(AutoServiceClientManager autoAutoServiceClientManager, RecyclableMemoryStreamManager streamManager)
    {
        _autoServiceClientManager = autoAutoServiceClientManager;
        _streamManager = streamManager;
        _serviceProxys = new Dictionary<string, Dictionary<ushort, InternalProxy>>();
        LoadProxys();
        InitializeServices();
    }
    private void LoadProxys()
    {
        _serviceProxys.Add("IAutoServiceClientSample", new Dictionary<ushort, InternalProxy>());
        
        _serviceProxys["IAutoServiceClientSample"].Add(0, InternalProxy_AutoServiceClientSample_IAutoServiceClientSample_0_ReceiveLink);
    }

    private void InitializeServices()
    {
        _autoServiceClientSample = new AutoServiceClientSample();
    }

    public Task OnConnectedAsync(INetXClientSession client)
    {
        return Task.CompletedTask;
    }

    public Task OnDisconnectedAsync()
    {
        return Task.CompletedTask;
    }

    public Task OnReceivedMessageAsync(INetXClientSession client, NetXMessage message)
    {
        var buffer = message.Buffer;
        var offset = buffer.Offset;
        
        buffer.Read(ref offset, out string interfaceCode);
        buffer.Read(ref offset, out ushort methodCode);

        Task.Run(async () =>
        {
            await _serviceProxys[interfaceCode][methodCode](client, message, offset);
        });
        return Task.CompletedTask;
    }

    public int GetReceiveMessageSize(INetXClientSession client, in ArraySegment<byte> buffer)
    {
        throw new NotImplementedException();
    }

    public void ProcessReceivedBuffer(INetXClientSession client, in ArraySegment<byte> buffer)
    {
        
    }

    public void ProcessSendBuffer(INetXClientSession client, in ArraySegment<byte> buffer)
    {
        
    }
    
    #region ServiceProviders

    private IAutoServiceClientSample _autoServiceClientSample;
    
    #endregion

    #region ServiceProxys

    private async ValueTask InternalProxy_AutoServiceClientSample_IAutoServiceClientSample_0_ReceiveLink(INetXClientSession client, NetXMessage message, int offset)
    {
        var inputBuffer = message.Buffer;
        inputBuffer.Read(ref offset, out ushort value);

        var autoServiceClientSample_ReceiveLink_Result = await _autoServiceClientSample.ReceiveLink(value);
        
        var stream = (RecyclableMemoryStream)_streamManager.GetStream("IAutoServiceClientSample_ReceiveLink", 4096, true);
        try
        {
            stream.Write(autoServiceClientSample_ReceiveLink_Result);
            
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