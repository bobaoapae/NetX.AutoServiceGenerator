using NetX.AutoServiceGenerator.Definitions;

namespace AutoServiceServerSample.Definitions;

public interface IAutoServiceSample
{
    Task<bool> TryDoSomething(string value, int value2, short value3, bool value5, byte[] value6);
    Task DoSomething([AutoServiceClientGuid] Guid guid);
}