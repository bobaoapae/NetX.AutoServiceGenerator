using System;
using System.Threading.Tasks;

namespace NetX
{
    public interface INetXServerProcessor
    {
        Task OnSessionConnectAsync(INetXSession session);
        Task OnSessionDisconnectAsync(Guid sessionId);
        Task OnReceivedMessageAsync(INetXSession session, NetXMessage message);

        int GetReceiveMessageSize(INetXSession session, in ArraySegment<byte> buffer);
        void ProcessReceivedBuffer(INetXSession session, in ArraySegment<byte> buffer);
        void ProcessSendBuffer(INetXSession session, in ArraySegment<byte> buffer);
    }
}
