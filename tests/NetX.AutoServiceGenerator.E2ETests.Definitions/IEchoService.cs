using System.Collections.Generic;
using System.Threading.Tasks;
using NetX.AutoServiceGenerator.Definitions;

namespace NetX.AutoServiceGenerator.E2ETests.Definitions;

[AutoServiceTimeout(Timeout = 5000)]
public interface IEchoService
{
    Task<EchoResult> Echo(string message);
    Task<int> Add(int a, int b);
    Task<List<int>> GetItems(int count);
    Task VoidMethod(string value);
    Task<bool> SlowMethod(int delayMs);
}
