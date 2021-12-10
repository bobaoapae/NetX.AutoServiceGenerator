using System.Threading;
using System.Threading.Tasks;
using Microsoft.IO;
using AutoServiceServerSample.Definitions;
using NetX;
using NetX.AutoServiceGenerator.Definitions;
using NetX.Options;
using System;

namespace AutoServiceClientSample;

public partial class AutoServiceClientManager
{

    private readonly RecyclableMemoryStreamManager manager;

    private string _address;
    private ushort _port;
    private INetXClient _netXClient;
    private AutoServiceClientManagerProcessor _processor;

    #region Services

    public IAutoServiceServerSample AutoServiceServerSample { get; }

    #endregion
    
    public AutoServiceClientManager(string address, ushort port)
    {
        _address = address;
        _port = port;
        int blockSize = 1024;
        int largeBufferMultiple = 1024 * 1024;
        int maxBufferSize = 16 * largeBufferMultiple;
        manager = new Microsoft.IO.RecyclableMemoryStreamManager(blockSize, largeBufferMultiple, maxBufferSize)
        {
            AggressiveBufferReturn = true,
            MaximumFreeSmallPoolBytes = blockSize * 2048,
            MaximumFreeLargePoolBytes = maxBufferSize * 4
        };
        _processor = new AutoServiceClientManagerProcessor(this, manager);
        _netXClient = NetXClientBuilder.Create()
            .Processor(_processor)
            .EndPoint(_address, _port)
            .Duplex(true)
            .CopyBuffer(true)
            .NoDelay(true)
            .ReceiveBufferSize(1024)
            .SendBufferSize(1024)
            .Build();

        #region InitializeServices

        AutoServiceServerSample = new AutoServiceServerSampleConsumer(_netXClient, manager);

        #endregion
    }

    public Task ConnectAsync(CancellationToken cancellationToken)
    {
        return _netXClient.ConnectAsync(cancellationToken);
    }

}