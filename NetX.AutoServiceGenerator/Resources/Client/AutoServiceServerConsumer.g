using NetX;
using System;
using Microsoft.IO;
using System.Threading.Tasks;
using AutoSerializer.Definitions;
using NetX.AutoServiceGenerator.Definitions;
using {4};

namespace {0};

public class {1}{2}ServerConsumer : I{1}
{{
    private INetXClient _client;
    private RecyclableMemoryStreamManager _streamManager;

    public {1}{2}ServerConsumer(INetXClient client, RecyclableMemoryStreamManager streamManager)
    {{
        _client = client;
        _streamManager = streamManager;
    }}

#region ServiceImplementation

{3}

#endregion
}}