using System;
using System.Threading.Tasks;
using AutoServiceClientSample.Definitions;
using NetX.AutoServiceGenerator.Definitions;
using Microsoft.IO;

namespace AutoServiceServerSample;

public class AutoServiceClientSampleConsumer : IAutoServiceClientSample
{
    private AutoServiceServerManagerSession _session;
    private RecyclableMemoryStreamManager _streamManager;

    public AutoServiceClientSampleConsumer(AutoServiceServerManagerSession session, RecyclableMemoryStreamManager streamManager)
    {
        _session = session;
        _streamManager = streamManager;
    }
    
    public async Task<bool> ReceiveLink(ushort value)
    {
        var stream = _streamManager.GetStream("AutoServiceClientConsumer_1_0_TryDoSomething", 4096, true);
        try
        {
            stream.Write(Convert.ToUInt16(1));
            stream.Write(Convert.ToUInt16(0));
            
            stream.Write(value);

            var autoServiceClientSample_1_0_ReceiveLink_Buffer_Result = await _session.Session.RequestAsync(stream);
            var autoServiceClientSample_1_0_ReceiveLink_Buffer_Result_Offset = autoServiceClientSample_1_0_ReceiveLink_Buffer_Result.Offset;
            
            autoServiceClientSample_1_0_ReceiveLink_Buffer_Result.Read(ref autoServiceClientSample_1_0_ReceiveLink_Buffer_Result_Offset, out bool autoServiceClientSample_1_0_ReceiveLink_Result);

            return autoServiceClientSample_1_0_ReceiveLink_Result;

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

    public Task DoSomethingWithSession()
    {
        throw new NotImplementedException();
    }
}