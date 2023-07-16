using System;
using System.Threading.Tasks;
using AutoServiceClientSample.Definitions;
using AutoServiceServerSample.Definitions;
using NetX;
using NetX.AutoServiceGenerator.Definitions;

namespace AutoServiceServerSample;

[AutoServiceServerAuthentication<Authentication, IpsAuthentication>]
[AutoServiceProvider(typeof(AutoServiceSample))]
[AutoServiceConsumer(typeof(IAutoServiceReceiverSample))]
public partial class AutoServiceServerManager : IAutoServiceServerManager
{
    public ValueTask OnSessionConnectAsync(AutoServiceServerManagerSession session)
    {
        Console.WriteLine($"Session Connected: {session.Session.Id}");
        return ValueTask.CompletedTask;
    }

    public ValueTask OnSessionDisconnectAsync(AutoServiceServerManagerSession session, DisconnectReason reason)
    {
        Console.WriteLine($"Session Disconnected: {session.Session.Id}");
        return ValueTask.CompletedTask;
    }
}