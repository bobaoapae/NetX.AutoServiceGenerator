using System;
using System.Threading.Tasks;
using NetX;

namespace ServerClientSample
{
    public class SampleServerProcessor : INetXServerProcessor
    {
        public Task OnSessionConnectAsync(INetXSession session)
        {
            Console.WriteLine($"Session {session.Id} connected. Time = {session.ConnectionTime} Address = {session.RemoteAddress}");

            return Task.CompletedTask;
        }

        public Task OnSessionDisconnectAsync(Guid sessionId)
        {
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
