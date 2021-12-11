using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoServiceServerSample.Definitions;

namespace AutoServiceServerSample;

public partial class AutoServiceServerSample : IAutoServiceServerSample
{
    public async Task<bool> TryDoSomething(string value, int value2, short value3, bool value5)
    {
        Console.WriteLine($"Invoked Service AutoServiceServerSample.TryDoSomething({value},{value2},{value3},{value5});");
        var result = value == "test" && value2 == 1000 && value3 == 45 &&  value5;

        if (TryGetCallingSession(out var session))
        {
            Console.WriteLine($"Invoking AutoServiceClientSample.ReceiveLink({value3})");
            result &= await session.AutoServiceClientSample.ReceiveLink(Convert.ToUInt16(value3));
        }
        else
        {
            result = false;
        }

        return result;

    }

    public Task<int[]> AppendValues(int value1, int value2, int value3, byte[] value4)
    {
        Console.WriteLine($"Invoked Service AutoServiceServerSample.AppendValues({value1},{value2},{value3},[{string.Join(",", value4)}]);");

        var result = new List<int>();
        result.Add(value1);
        result.Add(value2);
        result.Add(value3);
        result.AddRange(value4.Select(Convert.ToInt32));

        return Task.FromResult(result.ToArray());
    }
}