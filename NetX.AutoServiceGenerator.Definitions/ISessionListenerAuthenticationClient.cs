using System.Threading.Tasks;

namespace NetX.AutoServiceGenerator.Definitions;

public interface ISessionListenerAuthenticationClient<in T> : ISessionListenerClient where T: AutoServiceAuthenticationProto
{
    ValueTask OnAuthenticatedAsync(T authenticationProto);
}