using System;

namespace NetX
{
    public interface INetXServerProcessor : ISessionFactory<INetXSession>
    {
        int GetReceiveMessageSize(INetXSession session, in ArraySegment<byte> buffer);
        void ProcessReceivedBuffer(INetXSession session, in ArraySegment<byte> buffer);
        void OnReceivedMessage(INetXSession session, in NetXMessage message);
        void ProcessSendBuffer(INetXSession session, in ArraySegment<byte> buffer);
    }
}
