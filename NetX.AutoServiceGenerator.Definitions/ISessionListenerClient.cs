using System.Threading.Tasks;

namespace NetX.AutoServiceGenerator.Definitions;

public interface ISessionListenerClient
{
    Task OnConnectedAsync();

    Task OnDisconnectedAsync();
}