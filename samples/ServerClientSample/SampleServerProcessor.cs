using System;
using System.Threading.Tasks;
using NetX;

namespace ServerClientSample
{
    public class SampleServerProcessor : INetXServerProcessor
    {
        public Task OnSessionConnectAsync(INetXSession session)
        {
            Console.WriteLine($"[SERVER] Session {session.Id} connected. Time = {session.ConnectionTime} Address = {session.RemoteAddress}");

            return Task.CompletedTask;
        }

        public Task OnSessionDisconnectAsync(Guid sessionId)
        {
            Console.WriteLine($"[SERVER] Session {sessionId} disconnected");

            return Task.CompletedTask;
        }

        public Task OnReceivedMessageAsync(INetXSession session, NetXMessage message)
        {
            Console.WriteLine($"[SERVER] Received message from {session.Id}");

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
