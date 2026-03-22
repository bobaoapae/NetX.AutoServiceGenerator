using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NetX.AutoServiceGenerator.E2ETests.Definitions;

namespace NetX.AutoServiceGenerator.E2ETests;

public partial class EchoService : IEchoService
{
    public Task<EchoResult> Echo(string message)
    {
        return Task.FromResult(new EchoResult { Message = message, Length = message?.Length ?? 0 });
    }

    public Task<int> Add(int a, int b)
    {
        return Task.FromResult(a + b);
    }

    public Task<List<int>> GetItems(int count)
    {
        var items = Enumerable.Range(0, count).ToList();
        return Task.FromResult(items);
    }

    public Task VoidMethod(string value)
    {
        return Task.CompletedTask;
    }

    public async Task<bool> SlowMethod(int delayMs)
    {
        await Task.Delay(delayMs);
        return true;
    }
}
