using System;
using System.Threading.Tasks;
using AutoServiceClientSample.Definitions;
using AutoServiceServerSample.Definitions;
using NetX.AutoServiceGenerator.Definitions;

namespace AutoServiceServerSample;

[AutoServiceServerAuthentication<Authentication, IpsAuthentication, AutoServiceAuthenticationProto>]
[AutoServiceProvider(typeof(AutoServiceSample))]
[AutoServiceConsumer(typeof(IAutoServiceReceiverSample))]
public partial class AutoServiceServerManager : IAutoServiceServerManager
{
    public ValueTask OnSessionConnectAsync(AutoServiceServerManagerSession session)
    {
        Console.WriteLine($"Session Connected: {session.Session.Id}");
        return ValueTask.CompletedTask;
    }

    public ValueTask OnSessionDisconnectAsync(AutoServiceServerManagerSession session)
    {
        Console.WriteLine($"Session Disconnected: {session.Session.Id}");
        return ValueTask.CompletedTask;
    }
}