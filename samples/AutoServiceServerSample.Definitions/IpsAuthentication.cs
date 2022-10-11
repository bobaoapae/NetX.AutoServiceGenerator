using AutoSerializer.Definitions;

namespace AutoServiceServerSample.Definitions;

[AutoSerialize, AutoDeserialize]
public partial class IpsAuthentication
{
    public uint Id { get; set; }
}