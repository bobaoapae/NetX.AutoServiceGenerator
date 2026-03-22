using System.Threading.Tasks;
using NetX.AutoServiceGenerator.Definitions;
using NetX.AutoServiceGenerator.E2ETests.Definitions;

namespace NetX.AutoServiceGenerator.E2ETests;

public partial class TestAuthentication : IAutoServiceAuthentication<TestAuthProto, AutoServiceAuthenticationProto>
{
    public Task<AutoServiceAuthenticationProto> AuthenticateAsync(TestAuthProto authenticationProto)
    {
        var isAuthenticated = authenticationProto.UserId > 0 && authenticationProto.Token == "valid";

        return Task.FromResult(new AutoServiceAuthenticationProto
        {
            IsAuthenticated = isAuthenticated
        });
    }
}
