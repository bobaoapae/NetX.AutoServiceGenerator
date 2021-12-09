using System.Threading;
using Microsoft.IO;
using AutoServiceServerSample.Definitions;
using NetX;
using NetX.AutoServiceGenerator.Definitions;
using NetX.Options;
using System;

namespace AutoServiceServerSample;

public partial class AutoServiceServerManager
{

    private readonly RecyclableMemoryStreamManager manager;

    private string _address;
    private ushort _port;
    private INetXServer _netXServer;
    private AutoServiceManagerProcessor _processor;

    public AutoServiceServerManager(string address, ushort port)
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
        _processor = new AutoServiceManagerProcessor(this, manager, TryGetSession);
        _netXServer = NetXServerBuilder.Create()
            .Processor(_processor)
            .EndPoint(_address, _port)
            .Duplex(true)
            .NoDelay(true)
            .ReceiveBufferSize(1024)
            .SendBufferSize(1024)
            .Build();
    }

    public void StartListening(CancellationToken cancellationToken)
    {
        _netXServer.Listen(cancellationToken);
    }

    private bool TryGetSession(Guid guid, out AutoServiceServerManagerSession session)
    {
        if(_netXServer.TryGetSession(guid, out var iSession))
        {
            session = (AutoServiceServerManagerSession)iSession;
            return true;
        }

        session = null;

        return false;
    }
}