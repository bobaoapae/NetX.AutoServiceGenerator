using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NetX.AutoServiceGenerator.Definitions;

namespace AutoServiceServerSample.Definitions;

[AutoServiceTimeout(Timeout = -1)]
public interface IAutoServiceSample
{
    [AutoServiceTimeout(Timeout = 10000)]
    Task<bool> TryDoSomething(string value, int value2, short value3, bool value5);
    Task<List<int>> AppendValues(int value1, int value2, int value3, byte[] value4);
    Task MethodWithoutReturnValue(int value);
    Task MethodWithoutParameter();
}