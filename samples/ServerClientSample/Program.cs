using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NetX.Options;
using Serilog;

namespace ServerClientSample
{
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

            var server = NetXServerBuilder.Create(loggerFactory, "SampleServer")
                .Processor<SampleServerProcessor>()
                .EndPoint("0.0.0.0", 38101)
                .Duplex(true)
                .CopyBuffer(false)
                .NoDelay(true)
                .ReceiveBufferSize(1024)
                .SendBufferSize(1024)
                .Build();

            server.Listen(cancellationTokenSource.Token);

            var client = NetXClientBuilder.Create(loggerFactory, "SampleClient")
                .Processor<SampleClientProcessor>()
                .EndPoint("127.0.0.1", 38101)
                .Duplex(true)
                .NoDelay(true)
                .ReceiveBufferSize(1024)
                .SendBufferSize(1024)
                .Build();

            await client.ConnectAsync(cancellationTokenSource.Token);

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

            await Task.Delay(3000);
        }
    }
}