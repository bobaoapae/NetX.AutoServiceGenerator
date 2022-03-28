using System.Collections.Generic;
using System.Threading.Tasks;
using AutoServiceServerTest.Definitions;

namespace AutoServiceServerTest;

public partial class AutoServiceSample : IAutoServiceSample
{
    public Task<bool> TryDoSomething(string value, int value2, short value3, bool value5)
    {
        throw new System.NotImplementedException();
    }

    public Task<List<int>> AppendValues(int value1, int value2, int value3, byte[] value4)
    {
        throw new System.NotImplementedException();
    }

    public Task MethodWithoutReturnValue(int value)
    {
        throw new System.NotImplementedException();
    }

    public Task MethodWithoutParameter()
    {
        throw new System.NotImplementedException();
    }
}