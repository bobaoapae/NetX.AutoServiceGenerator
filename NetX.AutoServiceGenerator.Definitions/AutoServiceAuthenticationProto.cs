using AutoSerializer.Definitions;

namespace NetX.AutoServiceGenerator.Definitions;

[AutoSerialize, AutoDeserialize]
public partial class AutoServiceAuthenticationProto
{
    public bool IsAuthenticated { get; set; }
}