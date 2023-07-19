using System.Threading.Tasks;
using AutoServiceServerSample.Definitions;
using NetX.AutoServiceGenerator.Definitions;

namespace AutoServiceServerSample;

public partial class Authentication : IAutoServiceAuthentication<IpsAuthentication, AutoServiceAuthenticationProto>
{
    public Task<AutoServiceAuthenticationProto> AuthenticateAsync(IpsAuthentication authenticationProto)
    {
        return Task.FromResult(new AutoServiceAuthenticationProto
        {
            IsAuthenticated = authenticationProto.Id == 1
        });
    }
}