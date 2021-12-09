using AutoServiceServerSample.Definitions;
using NetX.AutoServiceGenerator.Definitions;

namespace AutoServiceClientSample;

[AutoServiceConsumer(typeof(IAutoServiceServerSample))]
public partial class AutoServiceClientManager : IAutoServiceClientManager
{
    
}