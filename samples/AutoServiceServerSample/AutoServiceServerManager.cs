using AutoServiceClientSample.Definitions;
using NetX.AutoServiceGenerator.Definitions;

namespace AutoServiceServerSample;

[AutoServiceConsumer(typeof(IAutoServiceClientSample))]
public partial class AutoServiceServerManager : IAutoServiceServerManager
{
    
}