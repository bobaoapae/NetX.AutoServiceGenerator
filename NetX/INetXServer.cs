using System;
using System.Threading;
using System.Collections.Generic;

namespace NetX
{
    public interface INetXServer
    {
        void Listen(CancellationToken cancellationToken = default);

        bool TryGetSession(Guid sessionId, out INetXSession session);
        IEnumerable<INetXSession> GetAllSessions();
    }
}
