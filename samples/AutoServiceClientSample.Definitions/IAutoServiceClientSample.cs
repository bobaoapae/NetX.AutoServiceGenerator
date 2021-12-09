using System.Threading.Tasks;
using NetX.AutoServiceGenerator.Definitions;

namespace AutoServiceClientSample.Definitions;

public interface IAutoServiceClientSample : IAutoService
{
    Task<bool> ReceiveLink(ushort value);
}