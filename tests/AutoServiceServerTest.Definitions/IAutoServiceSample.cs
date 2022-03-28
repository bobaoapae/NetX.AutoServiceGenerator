using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoServiceServerTest.Definitions;

public interface IAutoServiceSample
{
    Task<bool> TryDoSomething(string value, int value2, short value3, bool value5);
    Task<List<int>> AppendValues(int value1, int value2, int value3, byte[] value4);
    Task MethodWithoutReturnValue(int value);
    Task MethodWithoutParameter();
}