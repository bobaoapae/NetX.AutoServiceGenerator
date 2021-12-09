using System;
using System.Threading.Tasks;
using AutoServiceServerSample.Definitions;
using NetX.AutoServiceGenerator.Definitions;
using Microsoft.IO;

namespace AutoServiceServerSample;

public class AutoServiceSampleConsumer : IAutoServiceSample
{

    private AutoServiceManagerSession _session;
    private RecyclableMemoryStreamManager _streamManager;

    public AutoServiceSampleConsumer(AutoServiceManagerSession session, RecyclableMemoryStreamManager streamManager)
    {
        _session = session;
        _streamManager = streamManager;
    }
    
    public async Task<bool> TryDoSomething(string value, int value2, short value3, bool value5, byte[] value6)
    {
        var stream = (RecyclableMemoryStream)_streamManager.GetStream("AutoServiceSample_0_0_TryDoSomething", 4096, true);
        try
        {
            stream.Write(Convert.ToUInt16(0));
            stream.Write(Convert.ToUInt16(0));
            
            stream.Write(value);
            stream.Write(value2);
            stream.Write(value3);
            stream.Write(value5);
            stream.Write(value6);

            var autoServiceSample_0_0_TryDoSomething_Buffer_Result = await _session.RequestAsync(stream);
            var autoServiceSample_0_0_TryDoSomething_Buffer_Result_Offset = autoServiceSample_0_0_TryDoSomething_Buffer_Result.Offset;
            
            autoServiceSample_0_0_TryDoSomething_Buffer_Result.Read(ref autoServiceSample_0_0_TryDoSomething_Buffer_Result_Offset, out bool autoServiceSample_0_0_TryDoSomething_Result);

            return autoServiceSample_0_0_TryDoSomething_Result;

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

    public Task DoSomething(Guid guid)
    {
        throw new NotImplementedException();
    }
}