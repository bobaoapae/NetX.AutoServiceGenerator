using System;

namespace NetX
{
    public interface INetXClientProcessor
    {
        int GetReceiveMessageSize(INetXClientSession client, in ArraySegment<byte> buffer);
        void ProcessReceivedBuffer(INetXClientSession client, in ArraySegment<byte> buffer);
        void OnReceivedMessage(INetXClientSession client, in NetXMessage message);
        void ProcessSendBuffer(INetXClientSession client, in ArraySegment<byte> buffer);
    }
}
