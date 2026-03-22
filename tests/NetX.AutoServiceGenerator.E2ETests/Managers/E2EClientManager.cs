using System.Threading.Tasks;
using NetX;
using NetX.AutoServiceGenerator.Definitions;
using NetX.AutoServiceGenerator.E2ETests.Definitions;
namespace NetX.AutoServiceGenerator.E2ETests;

[AutoServiceClientAuthentication<TestAuthProto, AutoServiceAuthenticationProto>]
[AutoServiceProvider(typeof(NotificationService))]
[AutoServiceConsumer(typeof(IEchoService))]
public partial class E2EClientManager : IAutoServiceClientManager
{
    public bool Authenticated { get; private set; }
    public bool Disconnected { get; private set; }

    public ValueTask OnConnectedAsync()
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask OnDisconnectedAsync(DisconnectReason reason)
    {
        Disconnected = true;
        return ValueTask.CompletedTask;
    }

    public ValueTask OnAuthenticatedAsync(AutoServiceAuthenticationProto authenticationProto)
    {
        Authenticated = authenticationProto.IsAuthenticated;
        return ValueTask.CompletedTask;
    }
}
