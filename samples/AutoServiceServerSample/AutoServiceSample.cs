using System;
using System.Threading.Tasks;
using AutoServiceServerSample.Definitions;
using NetX.AutoServiceGenerator.Definitions;

namespace AutoServiceServerSample;

public partial class AutoServiceSample : IAutoServiceSample
{
    public Task<bool> TryDoSomething(string value, int value2, short value3, bool value5, byte[] value6)
    {
        throw new NotImplementedException();
    }

    public Task DoSomething([AutoServiceClientGuid] Guid guid)
    {
       AutoServiceManagerSession session =  _manager.GetSession(guid);
       session.AutoServiceSample
    }
}