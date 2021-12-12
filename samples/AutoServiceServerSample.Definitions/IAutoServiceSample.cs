using System.Threading.Tasks;
using NetX.AutoServiceGenerator.Definitions;

namespace AutoServiceServerSample.Definitions;

[AutoService]
public interface IAutoServiceSample
{
    Task<bool> TryDoSomething(string value, int value2, short value3, bool value5);
    Task<int[]> AppendValues(int value1, int value2, int value3, byte[] value4);
}