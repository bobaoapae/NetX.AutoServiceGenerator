using System;

namespace AutoServiceServerSample;

public partial class AutoServiceServerSample
{

    private AutoServiceManagerProcessor.TryGetCallingSession _tryGetCallingSession;
    private AutoServiceManagerProcessor.TryGetSession _tryGetSession;
    
    public AutoServiceServerSample(AutoServiceManagerProcessor.TryGetCallingSession tryGetCallingSession, AutoServiceManagerProcessor.TryGetSession tryGetSession)
    {
        _tryGetCallingSession = tryGetCallingSession;
        _tryGetSession = tryGetSession;
    }

    private bool TryGetCallingSession(out AutoServiceServerManagerSession session)
    {
        return _tryGetCallingSession(out session);
    }

    private bool TryGetSession(Guid guid, out AutoServiceServerManagerSession session)
    {
        return _tryGetSession(guid, out session);
    }
    
}