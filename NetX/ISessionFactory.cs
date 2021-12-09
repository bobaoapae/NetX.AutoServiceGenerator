using System;
using System.Net;

namespace NetX
{
    public interface ISessionFactory<T>
        where T : INetXSession
    {
        public T CreateSession(Guid sessionId, IPAddress remoteAddress);
    }
}
