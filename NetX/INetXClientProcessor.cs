using System;
using System.Threading.Tasks;

namespace NetX
{
    public interface INetXClientProcessor
    {
        Task OnConnectedAsync(INetXClientSession client);
        Task OnDisconnectedAsync();
        Task OnReceivedMessageAsync(INetXClientSession client, NetXMessage message);

        int GetReceiveMessageSize(INetXClientSession client, in ArraySegment<byte> buffer);
        void ProcessReceivedBuffer(INetXClientSession client, in ArraySegment<byte> buffer);
        void ProcessSendBuffer(INetXClientSession client, in ArraySegment<byte> buffer);
    }
}
