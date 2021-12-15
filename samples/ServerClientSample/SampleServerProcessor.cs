using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetX;

namespace ServerClientSample
{
    public class SampleServerProcessor : INetXServerProcessor
    {
        public async Task OnSessionConnectAsync(INetXSession session)
        {
            Console.WriteLine($"Session {session.Id} connected. Time = {session.ConnectionTime} Address = {session.RemoteAddress}");
        }

        public Task OnSessionDisconnectAsync(Guid sessionId)
        {
            Console.WriteLine($"Session {sessionId} disconnected");

            return Task.CompletedTask;
        }

        public async Task OnReceivedMessageAsync(INetXSession session, NetXMessage message)
        {
            var random = new Random();
            var bigText = string.Join("", Enumerable.Range(0, 1004).Select(x => random.Next(9).ToString()));
            await session.ReplyAsync(message.Id, Encoding.UTF8.GetBytes($"nicke{message.Buffer[0]}"));
        }

        public int GetReceiveMessageSize(INetXSession session, in ArraySegment<byte> buffer)
        {
            return 4;
        }

        public void ProcessReceivedBuffer(INetXSession session, in ArraySegment<byte> buffer)
        {
        }

        public void ProcessSendBuffer(INetXSession session, in ArraySegment<byte> buffer)
        {
        }
    }
}
