using NetX;
using System;
using Microsoft.IO;
using System.Threading.Tasks;
using AutoSerializer.Definitions;
using Microsoft.Extensions.Logging;
using NetX.AutoServiceGenerator.Definitions;
using {4};

namespace {0};

public class {1}{2}ServerConsumer : I{1}
{{
    private INetXClient _client;
    private readonly ILogger _logger;
    private RecyclableMemoryStreamManager _streamManager;

    public {1}{2}ServerConsumer(INetXClient client, ILogger logger, RecyclableMemoryStreamManager streamManager)
    {{
        _client = client;
        _logger = logger;
        _streamManager = streamManager;
    }}

#region ServiceImplementation

{3}

#endregion
}}