using System.Threading.Tasks;
using AutoSerializer.Definitions;

namespace NetX.AutoServiceGenerator.Definitions;

public interface IAutoServiceAuthentication<in T, TReturn> where T : IAutoSerialize, IAutoDeserialize where TReturn : AutoServiceAuthenticationProto
{
    Task<TReturn> AuthenticateAsync(T authenticationProto);
}