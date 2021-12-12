
using System;
using System.Threading.Tasks;
using AutoServiceClientSample.Definitions;

namespace AutoServiceClientSample;

public partial class AutoServiceReceiverSampleTwo : IAutoServiceReceiverSample
{
    public Task<bool> ReceiveLink(ushort value)
    {
        Console.WriteLine($"Invoked Service AutoServiceReceiverSampleTwo.ReceiveLink({value});");

        return Task.FromResult(value == 45);
    }
}