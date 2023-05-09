using System;
using System.Threading.Tasks;
using AutoServiceServerSample.Definitions;
using NetX.AutoServiceGenerator.Definitions;

namespace AutoServiceClientSample;

[AutoServiceClientAuthentication<IpsAuthentication>]
[AutoServiceProvider(typeof(AutoServiceReceiverSample))]
[AutoServiceConsumer(typeof(IAutoServiceSample))]
public partial class AutoServiceClientManager : IAutoServiceClientManager
{
    public ValueTask OnConnectedAsync()
    {
        Console.WriteLine("Connected");
        return ValueTask.CompletedTask;
    }

    public ValueTask OnDisconnectedAsync()
    {
        Console.WriteLine("Connected");
        return ValueTask.CompletedTask;
    }

    public ValueTask OnAuthenticatedAsync()
    {
        Console.WriteLine("Authenticated");
        return ValueTask.CompletedTask;
    }
}