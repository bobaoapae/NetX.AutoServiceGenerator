using System;
using System.Net;

namespace NetX
{
    public abstract class NetXServerProcessor<TSession> : INetXServerProcessor
        where TSession : NetXSession
    {
        INetXSession ISessionFactory<INetXSession>.CreateSession(Guid sessionId, IPAddress remoteAddress)
            => CreateSession(sessionId, remoteAddress);

        public abstract TSession CreateSession(Guid sessionId, IPAddress remoteAddress);

        public int GetReceiveMessageSize(INetXSession session, in ArraySegment<byte> buffer)
            => GetReceiveMessageSize((TSession)session, in buffer);
        protected virtual int GetReceiveMessageSize(TSession session, in ArraySegment<byte> buffer)
        {
            return 0;
        }

        public void ProcessReceivedBuffer(INetXSession session, in ArraySegment<byte> buffer)
            => ProcessReceivedBuffer((TSession)session, in buffer);

        protected virtual void ProcessReceivedBuffer(TSession session, in ArraySegment<byte> buffer)
        {
        }

        public void OnReceivedMessage(INetXSession session, in NetXMessage message)
            => OnReceivedMessage((TSession)session, in message);

        protected abstract void OnReceivedMessage(TSession session, in NetXMessage message);

        public void ProcessSendBuffer(INetXSession session, in ArraySegment<byte> buffer)
            => ProcessSendBuffer((TSession)session, in buffer);

        protected virtual void ProcessSendBuffer(TSession session, in ArraySegment<byte> buffer)
        {
        }
    }
}
