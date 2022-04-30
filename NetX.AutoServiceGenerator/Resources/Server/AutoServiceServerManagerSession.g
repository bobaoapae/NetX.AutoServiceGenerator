using NetX;
using System;
using System.Net;
using Microsoft.IO;
using Microsoft.Extensions.Logging;
{4}

namespace {0};

public partial class {1}Session
{{
    private RecyclableMemoryStreamManager _streamManager;

    #region DeclarationConsumerClientServices

{2}

    #endregion
    
    public INetXSession Session {{ get; }}
    private readonly ILogger _logger;

    public {1}Session(INetXSession session, ILogger logger, RecyclableMemoryStreamManager streamManager)
    {{
        Session = session;
        _streamManager = streamManager;
        _logger = logger;

        #region InitializeConsumerClientServices

{3}

        #endregion
    }}
    
    public void Disconnect()
    {{
        Session.Disconnect();
    }}
    
}}