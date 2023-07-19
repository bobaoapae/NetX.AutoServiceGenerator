using System.Threading.Tasks;

namespace NetX.AutoServiceGenerator.Definitions;

public interface ISessionListenerClient
{
    ValueTask OnConnectedAsync();

    ValueTask OnDisconnectedAsync(DisconnectReason reason);
}