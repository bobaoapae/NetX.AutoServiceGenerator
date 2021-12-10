using System;
using System.Net;

namespace NetX
{
    public interface INetXSession : INetXConnection
    {
        Guid Id { get; }
        IPAddress RemoteAddress { get; }
        DateTime ConnectionTime { get; }
    }
}
