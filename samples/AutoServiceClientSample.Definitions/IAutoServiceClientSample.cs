using System.Threading.Tasks;
using NetX.AutoServiceGenerator.Definitions;

namespace AutoServiceClientSample.Definitions;

[AutoService]
public interface IAutoServiceClientSample
{
    Task<bool> ReceiveLink(ushort value);
}