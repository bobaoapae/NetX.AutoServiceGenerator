using AutoServiceServerSample.Definitions;
using NetX.AutoServiceGenerator.Definitions;

namespace AutoServiceServerSample;

[AutoServiceConsumer(typeof(IAutoServiceSample))]
public partial class AutoServiceManager : IAutoServiceManager
{
    
}