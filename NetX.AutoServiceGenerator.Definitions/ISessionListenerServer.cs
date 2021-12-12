using System.Threading.Tasks;

namespace NetX.AutoServiceGenerator.Definitions;

public interface ISessionListenerServer<T>
{
    Task OnSessionConnectAsync(T session);

    Task OnSessionDisconnectAsync(T session);
}