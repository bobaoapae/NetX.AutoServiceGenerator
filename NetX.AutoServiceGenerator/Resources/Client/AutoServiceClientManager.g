using NetX;
using System;
using NetX.Options;
using Microsoft.IO;
using System.Threading;
using System.Threading.Tasks;
using NetX.AutoServiceGenerator.Definitions;
using Microsoft.Extensions.Logging;
{4}

namespace {0};

public partial class {1} : ISessionListenerClient
{{

    private readonly RecyclableMemoryStreamManager manager;

    private string _address;
    private ushort _port;
    private INetXClient _netXClient;
    private {1}Processor _processor;

    #region Services

 {2}

    #endregion
    
    public {1}(string address, ushort port, ILoggerFactory loggerFactory = null, int receiveBufferSize = 1024, int sendBufferSize = 1024)
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
        _processor = new {1}Processor(this, manager, OnConnectedAsync, OnDisconnectedAsync);
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

}}