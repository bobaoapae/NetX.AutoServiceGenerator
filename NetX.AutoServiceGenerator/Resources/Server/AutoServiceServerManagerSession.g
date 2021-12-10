using NetX;
using System;
using System.Net;
using Microsoft.IO;
{4}

namespace {0};

public class {1}Session
{{
    private RecyclableMemoryStreamManager _streamManager;

    #region DeclarationConsumerClientServices

{2}

    #endregion
    
    public INetXSession Session {{ get; }}

    public {1}Session(INetXSession session,  RecyclableMemoryStreamManager streamManager)
    {{
        Session = session;
        _streamManager = streamManager;

        #region InitializeConsumerClientServices

{3}

        #endregion
    }}
    
}}