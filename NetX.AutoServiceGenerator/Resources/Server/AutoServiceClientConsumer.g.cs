using System;
using Microsoft.IO;
using System.Threading.Tasks;
using AutoSerializer.Definitions;
using Microsoft.Extensions.Logging;
using NetX.AutoServiceGenerator.Definitions;
using {3};

namespace {0};

public class {1}{2}ClientConsumer : I{1}
{{
    private readonly {2}Session _session;
    private readonly ILogger _logger;
    private readonly RecyclableMemoryStreamManager _streamManager;

    public {1}{2}ClientConsumer({2}Session session, ILogger logger, RecyclableMemoryStreamManager streamManager)
    {{
        _session = session;
        _logger = logger;
        _streamManager = streamManager;
    }}

#region ServiceImplementation

{4}

#endregion

}}