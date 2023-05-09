using System.Threading.Tasks;

namespace NetX.AutoServiceGenerator.Definitions;

public interface ISessionListenerAuthenticationClient : ISessionListenerClient
{
    ValueTask OnAuthenticatedAsync();
}