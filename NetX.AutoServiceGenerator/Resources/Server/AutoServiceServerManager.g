using NetX;
using System;
using NetX.Options;
using Microsoft.IO;
using System.Threading;
using System.Threading.Tasks;
using NetX.AutoServiceGenerator.Definitions;
using Microsoft.Extensions.Logging;

namespace {0};

public partial class {1} : ISessionListenerServer<{1}Session>
{{

    private readonly RecyclableMemoryStreamManager manager;

    private readonly string _address;
    private readonly ushort _port;
    private readonly INetXServer _netXServer;
    private readonly {1}Processor _processor;
    private readonly ILogger _logger;
    private readonly string _identity;

    public {1}(string address, ushort port, ILoggerFactory loggerFactory = null, string identity = null, int receiveBufferSize = 1024, int sendBufferSize = 1024)
    {{
        _address = address;
        _port = port;
        _logger = loggerFactory?.CreateLogger<{1}>();
        _identity = identity == null ? nameof({1}) : identity;
        int blockSize = 1024;
        int largeBufferMultiple = 1024 * 1024;
        int maxBufferSize = 16 * largeBufferMultiple;
        manager = new Microsoft.IO.RecyclableMemoryStreamManager(blockSize, largeBufferMultiple, maxBufferSize)
        {{
            AggressiveBufferReturn = true,
            MaximumFreeSmallPoolBytes = blockSize * 2048,
            MaximumFreeLargePoolBytes = maxBufferSize * 4
        }};
        _processor = new {1}Processor(this, manager, _logger, _identity, OnSessionConnectAsync, OnSessionDisconnectAsync);
        _netXServer = NetXServerBuilder.Create(loggerFactory, nameof({1}))
            .Processor(_processor)
            .EndPoint(_address, _port)
            .Duplex(true)
            .CopyBuffer(true)
            .NoDelay(true)
            .ReceiveBufferSize(receiveBufferSize)
            .SendBufferSize(sendBufferSize)
            .Build();
    }}

    public void StartListening(CancellationToken cancellationToken)
    {{
        _netXServer.Listen(cancellationToken);
    }}
}}