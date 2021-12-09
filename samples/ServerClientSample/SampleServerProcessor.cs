using System;
using System.Net;
using System.Threading.Tasks;
using NetX;

namespace ServerClientSample
{
    public class SampleServerProcessor : NetXServerProcessor<SampleSession>
    {
        public override SampleSession CreateSession(Guid sessionId, IPAddress remoteAddress)
        {
            var session = new SampleSession
            {
                Teste = "Isso é um teste"
            };

            return session;
        }

        protected override void ProcessReceivedBuffer(SampleSession session, in ArraySegment<byte> buffer)
        {
            Console.WriteLine($"[SERVER] Processing received buffer");
        }

        protected override void OnReceivedMessage(SampleSession session, in NetXMessage message)
        {
            Console.WriteLine($"[SERVER] Received MessageId = {message.Id}, Length = {message.Buffer.Count}");
            Console.WriteLine("[SERVER] Replying to client");

            var receivedMessage = message;

            Task.Run(async () =>
            {
                var response = await session.RequestAsync(receivedMessage.Buffer);
                await session.ReplyAsync(receivedMessage.Id, response);
            }); 
        }

        protected override void ProcessSendBuffer(SampleSession session, in ArraySegment<byte> buffer)
        {
            Console.WriteLine($"[SERVER] Processing send buffer");
        }
    }
}
