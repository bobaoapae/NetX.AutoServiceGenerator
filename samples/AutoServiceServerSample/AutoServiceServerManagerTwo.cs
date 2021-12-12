using System;
using System.Threading.Tasks;
using AutoServiceClientSample.Definitions;
using NetX.AutoServiceGenerator.Definitions;

namespace AutoServiceServerSample;

[AutoServiceProvider(typeof(AutoServiceSampleTwo))]
[AutoServiceConsumer(typeof(IAutoServiceReceiverSample))]
public partial class AutoServiceServerManagerTwo : IAutoServiceServerManager
{
    public Task OnSessionConnectAsync(AutoServiceServerManagerTwoSession session)
    {
        Console.WriteLine($"Session Connected: {session.Session.Id}");
        return Task.CompletedTask;
    }

    public Task OnSessionDisconnectAsync(AutoServiceServerManagerTwoSession session)
    {
        Console.WriteLine($"Session Disconnected: {session.Session.Id}");
        return Task.CompletedTask;
    }
}