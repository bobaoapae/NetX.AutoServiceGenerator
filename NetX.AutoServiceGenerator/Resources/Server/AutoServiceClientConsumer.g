using System;
using Microsoft.IO;
using System.Threading.Tasks;
using AutoSerializer.Definitions;
using NetX.AutoServiceGenerator.Definitions;
using {3};

namespace {0};

public class {1}{2}ClientConsumer : I{1}
{{
    private {2}Session _session;
    private RecyclableMemoryStreamManager _streamManager;

    public {1}{2}ClientConsumer({2}Session session, RecyclableMemoryStreamManager streamManager)
    {{
        _session = session;
        _streamManager = streamManager;
    }}

#region ServiceImplementation

{4}

#endregion

}}