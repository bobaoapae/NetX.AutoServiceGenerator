using System.Threading.Tasks;
using NetX.AutoServiceGenerator.Definitions;

namespace AutoServiceServerSample.Definitions;

[AutoService]
public interface IAutoServiceServerSample
{
    Task<bool> TryDoSomething(string value, int value2, short value3, bool value5);
}