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
            await client.SendAsync(Encoding.UTF8.GetBytes("Requisicao 1"));
        }

        public Task OnDisconnectedAsync()
        {
            return Task.CompletedTask;
        }

        public async Task OnReceivedMessageAsync(INetXClientSession client, NetXMessage message)
        {
            var messageId = message.Id;
            var clientResponseBytes = Encoding.UTF8.GetBytes("Resposta final");

            await client.ReplyAsync(messageId, clientResponseBytes);
        }

        public int GetReceiveMessageSize(INetXClientSession client, in ArraySegment<byte> buffer)
        {
            throw new NotImplementedException();
        }

        public void ProcessReceivedBuffer(INetXClientSession client, in ArraySegment<byte> buffer)
        {
        }

        public void ProcessSendBuffer(INetXClientSession client, in ArraySegment<byte> buffer)
        {
        }
    }
}
