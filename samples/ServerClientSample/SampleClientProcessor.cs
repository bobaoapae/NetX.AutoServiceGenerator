using System;
using System.Text;
using System.Threading.Tasks;
using NetX;

namespace ServerClientSample
{
    public class SampleClientProcessor : INetXClientProcessor
    {
        public int GetReceiveMessageSize(INetXClientSession client, in ArraySegment<byte> buffer)
        {
            throw new NotImplementedException();
        }

        public void OnReceivedMessage(INetXClientSession client, in NetXMessage message)
        {
            Console.WriteLine($"[CLIENT] Received MessageId = {message.Id}, Length = {message.Buffer.Count}");

            var messageId = message.Id;
            var clientResponseBytes = Encoding.UTF8.GetBytes("Resposta final");

            Task.Run(async () => await client.ReplyAsync(messageId, clientResponseBytes));
        }

        public void ProcessReceivedBuffer(INetXClientSession client, in ArraySegment<byte> buffer)
        {
            Console.WriteLine($"[CLIENT] Processing received buffer");
        }

        public void ProcessSendBuffer(INetXClientSession client, in ArraySegment<byte> buffer)
        {
            Console.WriteLine($"[CLIENT] Processing send buffer");
        }
    }
}
