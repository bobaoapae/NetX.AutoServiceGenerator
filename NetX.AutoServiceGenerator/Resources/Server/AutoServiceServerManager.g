using NetX;
using System;
using NetX.Options;
using Microsoft.IO;
using System.Threading;
using NetX.AutoServiceGenerator.Definitions;

namespace {0};

public partial class {1}
{{

    private readonly RecyclableMemoryStreamManager manager;

    private string _address;
    private ushort _port;
    private INetXServer _netXServer;
    private {1}Processor _processor;

    public {1}(string address, ushort port)
    {{
        _address = address;
        _port = port;
        int blockSize = 1024;
        int largeBufferMultiple = 1024 * 1024;
        int maxBufferSize = 16 * largeBufferMultiple;
        manager = new Microsoft.IO.RecyclableMemoryStreamManager(blockSize, largeBufferMultiple, maxBufferSize)
        {{
            AggressiveBufferReturn = true,
            MaximumFreeSmallPoolBytes = blockSize * 2048,
            MaximumFreeLargePoolBytes = maxBufferSize * 4
        }};
        _processor = new {1}Processor(this, manager);
        _netXServer = NetXServerBuilder.Create()
            .Processor(_processor)
            .EndPoint(_address, _port)
            .Duplex(true)
            .CopyBuffer(true)
            .NoDelay(true)
            .ReceiveBufferSize(1024)
            .SendBufferSize(1024)
            .Build();
    }}

    public void StartListening(CancellationToken cancellationToken)
    {{
        _netXServer.Listen(cancellationToken);
    }}
}}