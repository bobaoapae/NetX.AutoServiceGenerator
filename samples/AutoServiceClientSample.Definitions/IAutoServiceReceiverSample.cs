using System.Threading.Tasks;
using NetX.AutoServiceGenerator.Definitions;

namespace AutoServiceClientSample.Definitions;

public interface IAutoServiceReceiverSample
{
    Task<bool> ReceiveLink(ushort value);
}