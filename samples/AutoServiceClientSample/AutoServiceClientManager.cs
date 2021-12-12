using AutoServiceServerSample.Definitions;
using NetX.AutoServiceGenerator.Definitions;

namespace AutoServiceClientSample;

[AutoServiceConsumer(typeof(IAutoServiceSample))]
public partial class AutoServiceClientManager : IAutoServiceClientManager
{
    
}