using System;
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

            await session.SendAsync(BitConverter.GetBytes(1));
            await session.SendAsync(BitConverter.GetBytes(2));
            await session.SendAsync(BitConverter.GetBytes(3));
        }

        public Task OnSessionDisconnectAsync(Guid sessionId)
        {
            Console.WriteLine($"Session {sessionId} disconnected");

            return Task.CompletedTask;
        }

        public Task OnReceivedMessageAsync(INetXSession session, NetXMessage message)
        {
            return Task.CompletedTask;
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
