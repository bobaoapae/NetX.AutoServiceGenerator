using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using NetX;
using NetX.AutoServiceGenerator.Definitions;
using NetX.AutoServiceGenerator.E2ETests.Definitions;
namespace NetX.AutoServiceGenerator.E2ETests;

[AutoServiceServerAuthentication<TestAuthentication, TestAuthProto, AutoServiceAuthenticationProto>]
[AutoServiceProvider(typeof(EchoService))]
[AutoServiceConsumer(typeof(INotificationService))]
public partial class E2EServerManager : IAutoServiceServerManager
{
    public ConcurrentBag<Guid> ConnectedSessions { get; } = new();
    public ConcurrentBag<Guid> DisconnectedSessions { get; } = new();

    public ValueTask OnSessionConnectAsync(E2EServerManagerSession session)
    {
        ConnectedSessions.Add(session.Session.Id);
        return ValueTask.CompletedTask;
    }

    public ValueTask OnSessionDisconnectAsync(E2EServerManagerSession session, DisconnectReason reason)
    {
        DisconnectedSessions.Add(session.Session.Id);
        return ValueTask.CompletedTask;
    }
}
