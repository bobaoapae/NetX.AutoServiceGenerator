using System;
using System.Threading.Tasks;
using AutoServiceClientSample.Definitions;

namespace AutoServiceClientSample;

public partial class AutoServiceClientSample : IAutoServiceClientSample
{
    public Task<bool> ReceiveLink(ushort value)
    {
        Console.WriteLine($"Invoked Service AutoServiceClientSample.ReceiveLink({value});");

        return Task.FromResult(value == 45);
    }
}