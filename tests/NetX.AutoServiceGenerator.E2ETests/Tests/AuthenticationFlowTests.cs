using System;
using System.Threading;
using System.Threading.Tasks;
using NetX.AutoServiceGenerator.E2ETests.Definitions;
using NetX.AutoServiceGenerator.E2ETests.Helpers;

using Xunit;

namespace NetX.AutoServiceGenerator.E2ETests.Tests;

public class AuthenticationFlowTests : IDisposable
{
    private readonly ushort _port;
    private readonly E2EServerManager _server;
    private readonly CancellationTokenSource _cts;
    private readonly Task _serverTask;

    public AuthenticationFlowTests()
    {
        _port = PortAllocator.GetNextPort();
        _cts = new CancellationTokenSource();
        _server = new E2EServerManager("127.0.0.1", _port);
        _serverTask = Task.Run(() => _server.StartListening(_cts.Token));
        Thread.Sleep(1000);
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }

    [Fact]
    public async Task ValidAuthentication_ShouldConnectSuccessfully()
    {
        var client = new E2EClientManager("127.0.0.1", _port);

        await client.ConnectAsync(new TestAuthProto { UserId = 1, Token = "valid" }, _cts.Token);

        Assert.True(client.IsConnected);
        Assert.True(client.Authenticated);

        client.Disconnect();
    }

    [Fact]
    public async Task ValidAuthentication_ShouldAllowServiceCalls()
    {
        var client = new E2EClientManager("127.0.0.1", _port);
        await client.ConnectAsync(new TestAuthProto { UserId = 1, Token = "valid" }, _cts.Token);

        var result = await client.EchoService.Echo("hello");

        Assert.Equal("hello", result.Message);
        client.Disconnect();
    }

    [Fact]
    public async Task InvalidAuthentication_ShouldThrowAndDisconnect()
    {
        var client = new E2EClientManager("127.0.0.1", _port);

        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await client.ConnectAsync(new TestAuthProto { UserId = 0, Token = "invalid" }, _cts.Token));
    }

    [Fact]
    public async Task InvalidToken_ShouldFailAuthentication()
    {
        var client = new E2EClientManager("127.0.0.1", _port);

        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await client.ConnectAsync(new TestAuthProto { UserId = 1, Token = "wrong" }, _cts.Token));
    }

    [Fact]
    public async Task ZeroUserId_ShouldFailAuthentication()
    {
        var client = new E2EClientManager("127.0.0.1", _port);

        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await client.ConnectAsync(new TestAuthProto { UserId = 0, Token = "valid" }, _cts.Token));
    }

    [Fact]
    public async Task AuthenticationTimeout_ShouldDisconnectRawClient()
    {
        using var rawClient = new RawNetXClient("127.0.0.1", _port);
        await rawClient.ConnectAsync(_cts.Token);
        await rawClient.Processor.WaitForConnected(_cts.Token);

        Assert.True(rawClient.Processor.IsConnected);

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await rawClient.Processor.WaitForDisconnected(timeoutCts.Token);

        Assert.True(rawClient.Processor.WasDisconnected);
    }

    [Fact]
    public async Task MultipleValidAuthentications_ShouldAllSucceed()
    {
        var clients = new E2EClientManager[5];
        for (int i = 0; i < clients.Length; i++)
        {
            clients[i] = new E2EClientManager("127.0.0.1", _port);
            await clients[i].ConnectAsync(new TestAuthProto { UserId = (uint)(i + 1), Token = "valid" }, _cts.Token);
        }

        for (int i = 0; i < clients.Length; i++)
        {
            Assert.True(clients[i].IsConnected);
            Assert.True(clients[i].Authenticated);
        }

        foreach (var client in clients)
            client.Disconnect();
    }
}
