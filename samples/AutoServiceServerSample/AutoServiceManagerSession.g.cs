using AutoServiceServerSample.Definitions;
using NetX;

namespace AutoServiceServerSample;

public class AutoServiceManagerSession : NetXSession
{
    
    public IAutoServiceSample AutoServiceSample { get; }

    public AutoServiceManagerSession()
    {
        AutoServiceSample = new AutoServiceSampleConsumer(this);
    }
    
}