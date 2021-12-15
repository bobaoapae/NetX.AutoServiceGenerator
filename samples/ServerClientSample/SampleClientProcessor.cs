using System;
using System.Text;
using System.Threading.Tasks;
using NetX;
using Serilog;

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
