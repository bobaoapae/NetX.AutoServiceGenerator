using NetX;
using System.Threading.Tasks;
using Microsoft.IO;
using NetX.AutoServiceGenerator.Definitions;
using System;
using System.Collections.Generic;

namespace AutoServiceServerSample;

public class AutoServiceManagerProcessor : NetXServerProcessor<AutoServiceManagerSession>
{
    private delegate ValueTask InternalProxy(AutoServiceManagerSession session, NetXMessage message, int offset);

    private readonly Dictionary<ushort, Dictionary<ushort, InternalProxy>> _serviceProxys;
    
    private AutoServiceManager _autoServiceManager;
    private RecyclableMemoryStreamManager _streamManager;

    public AutoServiceManagerProcessor(AutoServiceManager autoAutoServiceManager, RecyclableMemoryStreamManager streamManager)
    {
        _autoServiceManager = autoAutoServiceManager;
        _streamManager = streamManager;
        _serviceProxys = new Dictionary<ushort, Dictionary<ushort, InternalProxy>>();
        LoadProxys();
    }

    private void LoadProxys()
    {
        if(_serviceProxys.ContainsKey(0))
            _serviceProxys.Add(0, new Dictionary<ushort, InternalProxy>());
        if(_serviceProxys[0].ContainsKey(0))
            _serviceProxys[0].Add(0, InternalProxy_AutoServiceSample_0_0_TryDoSomething);
    }

    public override AutoServiceManagerSession CreateSession()
    {
        return new AutoServiceManagerSession();
    }

    protected override void OnReceivedMessage(AutoServiceManagerSession session, in NetXMessage message)
    {
        var buffer = message.Buffer;
        var offset = buffer.Offset;
        
        buffer.Read(ref offset, out ushort interfaceCode);
        buffer.Read(ref offset, out ushort methodCode);

        var xMessage = message;
        Task.Run(async () => await _serviceProxys[interfaceCode][methodCode](session, xMessage, offset));
    }

    #region ServiceProxys

    private async ValueTask InternalProxy_AutoServiceSample_0_0_TryDoSomething(AutoServiceManagerSession session, NetXMessage message, int offset)
    {
        var inputBuffer = message.Buffer;
        inputBuffer.Read(ref offset, out string value);
        inputBuffer.Read(ref offset, out int value2);
        inputBuffer.Read(ref offset, out short value3);
        inputBuffer.Read(ref offset, out bool value5);
        inputBuffer.Read(ref offset, out byte[] value6);
        
        var autoServiceSample_0_0_TryDoSomething_Result = await _autoServiceManager.AutoServiceSample.TryDoSomething(value, value2, value3, value5, value6);
        
        var stream = (RecyclableMemoryStream)_streamManager.GetStream("AutoServiceSample_0_0_TryDoSomething", 4096, true);
        try
        {
            stream.Write(Convert.ToUInt16(0));
            stream.Write(Convert.ToUInt16(0));
            
            stream.Write(autoServiceSample_0_0_TryDoSomething_Result);
            
            await session.ReplyAsync(message.Id, stream);
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