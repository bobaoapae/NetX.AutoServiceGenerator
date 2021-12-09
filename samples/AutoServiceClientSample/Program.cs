using System;
using System.Threading;
using System.Threading.Tasks;

namespace AutoServiceClientSample;

public class Program
{
    public static async Task Main(string[] args)
    {
        var cancellationTokenSource = new CancellationTokenSource();

        var serviceManager = new AutoServiceClientManager("127.0.0.1", 2000);
        await serviceManager.ConnectAsync(cancellationTokenSource.Token);

        var result = await serviceManager.AutoServiceServerSample.TryDoSomething("test", 1000, 45, true);

        while (!cancellationTokenSource.IsCancellationRequested)
        {
            var command = Console.ReadLine();

            if (command == "stop")
            {
                Console.WriteLine("Stopping");
                cancellationTokenSource.Cancel();
            }

            await Task.Yield();
        }
    }
}