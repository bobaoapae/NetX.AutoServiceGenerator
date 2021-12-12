using System;
using System.Text;
using System.Threading.Tasks;
using NetX;

namespace ServerClientSample
{
    public class SampleClientProcessor : INetXClientProcessor
    {
        public async Task OnConnectedAsync(INetXClientSession client)
        {
            //await client.SendAsync(Encoding.UTF8.GetBytes("Requisicao 1"));
        }

        public Task OnDisconnectedAsync()
        {
            return Task.CompletedTask;
        }

        public Task OnReceivedMessageAsync(INetXClientSession client, NetXMessage message)
        {
            var recebeu = BitConverter.ToInt32(message.Buffer);
            Console.WriteLine($"Received from server: {recebeu}");

            return Task.CompletedTask;
        }

        public int GetReceiveMessageSize(INetXClientSession client, in ArraySegment<byte> buffer)
        {
            return 4;
        }

        public void ProcessReceivedBuffer(INetXClientSession client, in ArraySegment<byte> buffer)
        {
        }

        public void ProcessSendBuffer(INetXClientSession client, in ArraySegment<byte> buffer)
        {
        }
    }
}
