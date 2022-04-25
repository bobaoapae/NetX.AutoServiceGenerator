using NetX;
using System;
using NetX.Options;
using Microsoft.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NetX.AutoServiceGenerator.Definitions;
{4}

namespace {0};

public partial class {1} : ISessionListenerClient
{{

    private readonly RecyclableMemoryStreamManager manager;

    private readonly string _address;
    private readonly ushort _port;
    private readonly INetXClient _netXClient;
    private readonly {1}Processor _processor;
    private readonly ILogger _logger;
    private readonly string _identity;

    #region Services

 {2}

    #endregion
    
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
        _processor = new {1}Processor(this, manager, _logger, _identity, OnConnectedAsync, OnDisconnectedAsync);
        _netXClient = NetXClientBuilder.Create(loggerFactory, nameof({1}))
            .Processor(_processor)
            .EndPoint(_address, _port)
            .Duplex(true)
            .CopyBuffer(true)
            .NoDelay(true)
            .ReceiveBufferSize(receiveBufferSize)
            .SendBufferSize(sendBufferSize)
            .Build();

        #region InitializeServices

{3}

        #endregion
    }}

    public Task ConnectAsync(CancellationToken cancellationToken)
    {{
        return _netXClient.ConnectAsync(cancellationToken);
    }}
    
    public void Disconnect()
    {{
        _netXClient.Disconnect();
    }}

}}