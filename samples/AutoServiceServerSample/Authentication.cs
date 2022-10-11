using System.Threading.Tasks;
using AutoServiceServerSample.Definitions;
using NetX.AutoServiceGenerator.Definitions;

namespace AutoServiceServerSample;

public partial class Authentication : IAutoServiceAuthentication<IpsAuthentication>
{
    public Task<bool> AuthenticateAsync(IpsAuthentication authenticationProto)
    {
        return Task.FromResult(authenticationProto.Id == 1);
    }
}