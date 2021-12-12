using AutoServiceClientSample.Definitions;
using NetX.AutoServiceGenerator.Definitions;

namespace AutoServiceServerSample;

[AutoServiceConsumer(typeof(IAutoServiceReceiverSample))]
public partial class AutoServiceServerManager : IAutoServiceServerManager
{
    
}