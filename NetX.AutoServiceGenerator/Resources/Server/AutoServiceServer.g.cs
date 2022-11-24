using System;

namespace {0};

public partial class {1}
{{

    private {2}Processor.TryGetCallingSession _tryGetCallingSession;
    private {2}Processor.TryGetSession _tryGetSession;
   
    private {2}Session CurrentSession => TryGetCallingSession(out var session) ? session : null;
    
    public {1}({2}Processor.TryGetCallingSession tryGetCallingSession, {2}Processor.TryGetSession tryGetSession)
    {{
        _tryGetCallingSession = tryGetCallingSession;
        _tryGetSession = tryGetSession;
    }}

    private bool TryGetCallingSession(out {2}Session session)
    {{
        return _tryGetCallingSession(out session);
    }}

    private bool TryGetSession(Guid guid, out {2}Session session)
    {{
        return _tryGetSession(guid, out session);
    }}
    
}}