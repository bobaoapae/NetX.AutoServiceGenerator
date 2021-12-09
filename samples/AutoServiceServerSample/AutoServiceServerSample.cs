using System;
using System.Linq;
using System.Threading.Tasks;
using AutoServiceServerSample.Definitions;

namespace AutoServiceServerSample;

public partial class AutoServiceServerSample : IAutoServiceServerSample
{
    public async Task<bool> TryDoSomething(string value, int value2, short value3, bool value5)
    {
        //expected values "test", 1000, 45, true
        
        var result = value == "test" && value2 == 1000 && value3 == 45 &&  value5;

        if (TryGetCallingSession(out var session))
        {
            result &= await session.AutoServiceSample.ReceiveLink(Convert.ToUInt16(value3));
        }
        else
        {
            result = false;
        }

        return result;

    }
}