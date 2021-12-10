using System;
using System.Threading.Tasks;
using AutoServiceClientSample.Definitions;
using AutoServiceServerSample.Definitions;
using NetX.AutoServiceGenerator.Definitions;
using Microsoft.IO;
using NetX;

namespace AutoServiceClientSample;

public class AutoServiceServerSampleConsumer : IAutoServiceServerSample
{
    private INetXClient _client;
    private RecyclableMemoryStreamManager _streamManager;

    public AutoServiceServerSampleConsumer(INetXClient client, RecyclableMemoryStreamManager streamManager)
    {
        _client = client;
        _streamManager = streamManager;
    }

    public async Task<bool> TryDoSomething(string value, int value2, short value3, bool value5)
    {
        var stream = (RecyclableMemoryStream)_streamManager.GetStream("IAutoServiceServerSample_TryDoSomething", 4096, true);
        try
        {
            stream.Write("IAutoServiceServerSample");
            stream.Write(Convert.ToUInt16(0));
            
            stream.Write(value);
            stream.Write(value2);
            stream.Write(value3);
            stream.Write(value5);

            var autoServiceSample_0_0_TryDoSomething_Buffer_Result = await _client.RequestAsync(stream);
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

    public Task DoSomethingWithSession()
    {
        throw new NotImplementedException();
    }
}