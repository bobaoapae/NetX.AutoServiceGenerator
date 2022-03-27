using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Serilog;

namespace AutoServiceClientSample;

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

        var serviceManager = new AutoServiceClientManager("127.0.0.1", 2000, loggerFactory);
        await serviceManager.ConnectAsync(cancellationTokenSource.Token);

        Console.WriteLine($"Invoking AutoServiceServerSample.TryDoSomething(\"test\", 1000, 45, true)");
        var result = await serviceManager.AutoServiceSample.TryDoSomething("test", 1000, 45, true);
        
        Console.WriteLine($"Final Result: {result}");

        var appendResult = await serviceManager.AutoServiceSample.AppendValues(1, 2, 3, new byte[] { 4, 5, 6, 7, 8, 9 });
        
        Console.WriteLine($"Append Result: {string.Join(",", appendResult)}");

        Console.WriteLine($"Invoking AutoServiceServerSample.MethodWithoutReturnValue(1)");
        await serviceManager.AutoServiceSample.MethodWithoutReturnValue(1);
        
        var serviceManagerTwo = new AutoServiceClientManagerTwo("127.0.0.1", 2001, loggerFactory);
        await serviceManagerTwo.ConnectAsync(cancellationTokenSource.Token);

        Console.WriteLine($"Invoking AutoServiceServerSample.TryDoSomething(\"test\", 1000, 45, true)");
        result = await serviceManagerTwo.AutoServiceSample.TryDoSomething("test", 1000, 45, true);
        
        Console.WriteLine($"Final Result: {result}");

        appendResult = await serviceManagerTwo.AutoServiceSample.AppendValues(1, 2, 3, new byte[] { 4, 5, 6, 7, 8, 9 });
        
        Console.WriteLine($"Append Result: {string.Join(",", appendResult)}");

        try
        {
            await serviceManagerTwo.AutoServiceSample.MethodWithoutReturnValue(1);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        await serviceManager.AutoServiceSample.MethodWithoutParameter();

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