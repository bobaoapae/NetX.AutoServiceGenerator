using System.Threading.Tasks;

namespace NetX.AutoServiceGenerator.Definitions;

public interface ISessionListenerServer<T>
{
    ValueTask OnSessionConnectAsync(T session);

    ValueTask OnSessionDisconnectAsync(T session);
}