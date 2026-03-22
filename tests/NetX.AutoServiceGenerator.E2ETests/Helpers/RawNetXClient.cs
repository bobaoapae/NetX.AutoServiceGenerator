using System;
using System.Threading;
using System.Threading.Tasks;
using NetX;
using NetX.Options;

namespace NetX.AutoServiceGenerator.E2ETests.Helpers;

public class RawClientProcessor : INetXClientProcessor
{
    private readonly TaskCompletionSource _connectedTcs = new();
    private readonly TaskCompletionSource _disconnectedTcs = new();

    public bool IsConnected { get; private set; }
    public bool WasDisconnected { get; private set; }
    public DisconnectReason? LastDisconnectReason { get; private set; }

    public Task WaitForConnected(CancellationToken ct = default)
    {
        ct.Register(() => _connectedTcs.TrySetCanceled());
        return _connectedTcs.Task;
    }

    public Task WaitForDisconnected(CancellationToken ct = default)
    {
        ct.Register(() => _disconnectedTcs.TrySetCanceled());
        return _disconnectedTcs.Task;
    }

    public ValueTask OnConnectedAsync(INetXClientSession client, CancellationToken cancellationToken)
    {
        IsConnected = true;
        _connectedTcs.TrySetResult();
        return ValueTask.CompletedTask;
    }

    public ValueTask OnDisconnectedAsync(DisconnectReason reason)
    {
        IsConnected = false;
        WasDisconnected = true;
        LastDisconnectReason = reason;
        _disconnectedTcs.TrySetResult();
        return ValueTask.CompletedTask;
    }

    public ValueTask OnReceivedMessageAsync(INetXClientSession client, NetXMessage message, CancellationToken cancellationToken)
    {
        message.Dispose();
        return ValueTask.CompletedTask;
    }

    public int GetReceiveMessageSize(INetXClientSession client, in ReadOnlyMemory<byte> buffer)
    {
        throw new NotImplementedException();
    }

    public void ProcessReceivedBuffer(INetXClientSession client, in ReadOnlyMemory<byte> buffer)
    {
    }

    public void ProcessSendBuffer(INetXClientSession client, in ReadOnlyMemory<byte> buffer)
    {
    }
}

public class RawNetXClient : IDisposable
{
    private readonly INetXClient _client;
    public RawClientProcessor Processor { get; }

    public RawNetXClient(string address, ushort port)
    {
        Processor = new RawClientProcessor();
        _client = NetXClientBuilder.Create(null, "RawTestClient")
            .Processor(Processor)
            .EndPoint(address, port)
            .Duplex(true, 30000)
            .ReceiveBufferSize(1024)
            .SendBufferSize(1024)
            .Build();
    }

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        await _client.ConnectAsync(ct);
    }

    public void Disconnect()
    {
        _client.Disconnect();
    }

    public void Dispose()
    {
        try { _client.Disconnect(); } catch { }
    }
}
