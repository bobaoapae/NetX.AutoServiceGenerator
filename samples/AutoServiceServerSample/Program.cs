using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Serilog;

namespace AutoServiceServerSample;

public class Program
{
    public static async Task Main(string[] args)
    {
        var cancellationTokenSource = new CancellationTokenSource();
        
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(Serilog.Events.LogEventLevel.Verbose)
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:l}{NewLine}{Exception}")
            .CreateLogger();

        var loggerFactory = new LoggerFactory()
            .AddSerilog(Log.Logger);

        var serviceManager = new AutoServiceServerManager("0.0.0.0", 2000, loggerFactory);
        serviceManager.StartListening(cancellationTokenSource.Token);

        var serviceManagerTwo = new AutoServiceServerManagerTwo("0.0.0.0", 2001, loggerFactory);
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