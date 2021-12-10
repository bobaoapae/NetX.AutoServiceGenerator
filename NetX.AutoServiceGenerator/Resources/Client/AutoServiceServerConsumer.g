using NetX;
using System;
using Microsoft.IO;
using System.Threading.Tasks;
using NetX.AutoServiceGenerator.Definitions;
using {2};

namespace {0};

public class {1}ServerConsumer : I{1}
{{
    private INetXClient _client;
    private RecyclableMemoryStreamManager _streamManager;

    public {1}ServerConsumer(INetXClient client, RecyclableMemoryStreamManager streamManager)
    {{
        _client = client;
        _streamManager = streamManager;
    }}

#region ServiceImplementation

{3}

#endregion
}}