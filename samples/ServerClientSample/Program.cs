using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NetX.Options;

namespace ServerClientSample
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var cancellationTokenSource = new CancellationTokenSource();

            var server = NetXServerBuilder.Create()
                .Processor<SampleServerProcessor>()
                .EndPoint("0.0.0.0", 38101)
                .Duplex(true)
                .NoDelay(true)
                .ReceiveBufferSize(1024)
                .SendBufferSize(1024)
                .Build();

            server.Listen(cancellationTokenSource.Token);
            Console.WriteLine("Server started");

            var client = NetXClientBuilder.Create()
                .Processor<SampleClientProcessor>()
                .EndPoint("127.0.0.1", 38101)
                .Duplex(true)
                .NoDelay(true)
                .ReceiveBufferSize(1024)
                .SendBufferSize(1024)
                .Build();

            await client.ConnectAsync(cancellationTokenSource.Token);
            Console.WriteLine("Client connected");

            await Task.Delay(100);

            var responseBytes = await client.RequestAsync(Encoding.UTF8.GetBytes("Requisicao 1"));
            var response = Encoding.UTF8.GetString(responseBytes);

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
}