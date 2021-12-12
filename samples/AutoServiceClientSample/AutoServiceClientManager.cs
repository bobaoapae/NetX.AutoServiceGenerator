using System;
using System.Threading.Tasks;
using AutoServiceServerSample.Definitions;
using NetX.AutoServiceGenerator.Definitions;

namespace AutoServiceClientSample;

[AutoServiceConsumer(typeof(IAutoServiceSample))]
public partial class AutoServiceClientManager : IAutoServiceClientManager
{
    public Task OnConnectedAsync()
    {
        Console.WriteLine("Connected");
        return Task.CompletedTask;
    }

    public Task OnDisconnectedAsync()
    {
        Console.WriteLine("Connected");
        return Task.CompletedTask;
    }
}