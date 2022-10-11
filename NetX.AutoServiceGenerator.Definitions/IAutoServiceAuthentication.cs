using System.Threading.Tasks;
using AutoSerializer.Definitions;

namespace NetX.AutoServiceGenerator.Definitions;

public interface IAutoServiceAuthentication<in T> where T : IAutoSerialize, IAutoDeserialize
{
    Task<bool> AuthenticateAsync(T authenticationProto);
}