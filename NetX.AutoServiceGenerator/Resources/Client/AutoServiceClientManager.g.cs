using NetX;
using System;
using NetX.Options;
using Microsoft.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using NetX.AutoServiceGenerator.Definitions;
{4}

namespace {0};

public partial class {1} : {7}
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

    public bool IsConnected => _netXClient.IsConnected;
    
    public {1}(string address, ushort port, ILoggerFactory loggerFactory = null, string identity = null, int recvBufferSize = 1024, int sendBufferSize = 1024, bool noDelay = false, int socketTimeout = 5000, bool disconnectOnTimeout = false)
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
            .Duplex(true, socketTimeout)
            .CopyBuffer(true)
            .NoDelay(noDelay)
            .ReceiveBufferSize(recvBufferSize)
            .SendBufferSize(sendBufferSize)
            .SocketTimeout(socketTimeout)
            .DisconnectOnTimeout(disconnectOnTimeout)
            .Build();

        #region InitializeServices

{3}

        #endregion
    }}

    public async Task ConnectAsync({5}CancellationToken cancellationToken)
    {{
        await _netXClient.ConnectAsync(cancellationToken);
{6}
    }}
    
    public void Disconnect()
    {{
        _netXClient.Disconnect();
    }}

}}