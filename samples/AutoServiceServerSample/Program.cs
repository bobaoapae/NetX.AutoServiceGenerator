using System;
using System.Threading;
using System.Threading.Tasks;

namespace AutoServiceServerSample;

public class Program
{
    public static async Task Main(string[] args)
    {
        var cancellationTokenSource = new CancellationTokenSource();

        var serviceManager = new AutoServiceServerManager("0.0.0.0", 2000);
        serviceManager.StartListening(cancellationTokenSource.Token);

        var serviceManagerTwo = new AutoServiceServerManagerTwo("0.0.0.0", 2001);
        serviceManagerTwo.StartListening(cancellationTokenSource.Token);


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