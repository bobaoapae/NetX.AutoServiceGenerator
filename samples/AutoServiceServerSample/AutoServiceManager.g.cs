using System.Threading;
using Microsoft.IO;
using AutoServiceServerSample.Definitions;
using NetX;
using NetX.AutoServiceGenerator.Definitions;
using NetX.Options;

namespace AutoServiceServerSample;

public partial class AutoServiceManager
{
    private readonly RecyclableMemoryStreamManager manager;
    public static AutoServiceManager I = new();

    private INetXServer _netXServer;
    private AutoServiceManagerProcessor _processor;

    public AutoServiceManager()
    {
        int blockSize = 1024;
        int largeBufferMultiple = 1024 * 1024;
        int maxBufferSize = 16 * largeBufferMultiple;
        manager = new Microsoft.IO.RecyclableMemoryStreamManager(blockSize, largeBufferMultiple, maxBufferSize)
        {
            AggressiveBufferReturn = true,
            MaximumFreeSmallPoolBytes = blockSize * 2048,
            MaximumFreeLargePoolBytes = maxBufferSize * 4
        };
        
        AutoServiceSample = new AutoServiceSample(this);
    }

    public void StartListening(string address, ushort port, CancellationToken cancellationToken)
    {
        _processor = new AutoServiceManagerProcessor(this, manager);
        _netXServer = NetXServerBuilder.Create()
            .Processor(_processor)
            .EndPoint(address, port)
            .Duplex(true)
            .NoDelay(true)
            .ReceiveBufferSize(1024)
            .SendBufferSize(1024)
            .Build();
        _netXServer.Listen(cancellationToken);
    }
    
    #region ServiceProviders

    public IAutoServiceSample AutoServiceSample { get; }

    #endregion
}