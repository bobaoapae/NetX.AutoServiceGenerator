using AutoSerializer.Definitions;

namespace NetX.AutoServiceGenerator.E2ETests.Definitions;

[AutoSerialize, AutoDeserialize]
public partial class TestAuthProto
{
    public uint UserId { get; set; }
    public string Token { get; set; }
}
