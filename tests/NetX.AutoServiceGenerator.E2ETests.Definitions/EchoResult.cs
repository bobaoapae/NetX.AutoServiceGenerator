using AutoSerializer.Definitions;

namespace NetX.AutoServiceGenerator.E2ETests.Definitions;

[AutoSerialize, AutoDeserialize]
public partial class EchoResult
{
    public string Message { get; set; }
    public int Length { get; set; }
}
