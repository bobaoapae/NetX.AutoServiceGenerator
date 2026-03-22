using System;
using System.Threading;
using System.Threading.Tasks;
using NetX.AutoServiceGenerator.E2ETests.Definitions;
using NetX.AutoServiceGenerator.E2ETests.Helpers;

using Xunit;

namespace NetX.AutoServiceGenerator.E2ETests.Tests;

public class ReconnectionTests : IDisposable
{
    private readonly ushort _port;
    private readonly E2EServerManager _server;
    private readonly CancellationTokenSource _cts;

    public ReconnectionTests()
    {
        _port = PortAllocator.GetNextPort();
        _cts = new CancellationTokenSource();
        _server = new E2EServerManager("127.0.0.1", _port);
        _ = Task.Run(() => _server.StartListening(_cts.Token));
        Thread.Sleep(1000);
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }

    [Fact]
    public async Task NewClientAfterDisconnect_ShouldWork()
    {
        // First connection
        var client1 = new E2EClientManager("127.0.0.1", _port);
        await client1.ConnectAsync(new TestAuthProto { UserId = 1, Token = "valid" }, _cts.Token);

        var result1 = await client1.EchoService.Echo("first_connection");
        Assert.Equal("first_connection", result1.Message);
        client1.Disconnect();

        await Task.Delay(500);

        // Second connection with new client instance
        var client2 = new E2EClientManager("127.0.0.1", _port);
        await client2.ConnectAsync(new TestAuthProto { UserId = 2, Token = "valid" }, _cts.Token);

        var result2 = await client2.EchoService.Echo("second_connection");
        Assert.Equal("second_connection", result2.Message);
        client2.Disconnect();
    }

    [Fact]
    public async Task NewClientAfterAuthFailure_ShouldWork()
    {
        // Failed auth attempt
        var failedClient = new E2EClientManager("127.0.0.1", _port);
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await failedClient.ConnectAsync(new TestAuthProto { UserId = 0, Token = "invalid" }, _cts.Token));

        await Task.Delay(500);

        // Successful connection
        var successClient = new E2EClientManager("127.0.0.1", _port);
        await successClient.ConnectAsync(new TestAuthProto { UserId = 1, Token = "valid" }, _cts.Token);

        var result = await successClient.EchoService.Echo("after_failed_auth");
        Assert.Equal("after_failed_auth", result.Message);
        successClient.Disconnect();
    }

    [Fact]
    public async Task MultipleReconnections_ShouldAllSucceed()
    {
        for (int i = 0; i < 5; i++)
        {
            var client = new E2EClientManager("127.0.0.1", _port);
            await client.ConnectAsync(new TestAuthProto { UserId = (uint)(i + 1), Token = "valid" }, _cts.Token);

            var result = await client.EchoService.Echo($"reconnect_{i}");
            Assert.Equal($"reconnect_{i}", result.Message);

            client.Disconnect();
            await Task.Delay(300);
        }
    }

    [Fact]
    public async Task ReconnectAfterRawClientTimeout_ShouldWork()
    {
        // Raw client will timeout
        using var rawClient = new RawNetXClient("127.0.0.1", _port);
        await rawClient.ConnectAsync(_cts.Token);
        await rawClient.Processor.WaitForConnected(_cts.Token);

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await rawClient.Processor.WaitForDisconnected(timeoutCts.Token);

        // Now connect a legitimate client
        var client = new E2EClientManager("127.0.0.1", _port);
        await client.ConnectAsync(new TestAuthProto { UserId = 1, Token = "valid" }, _cts.Token);

        var result = await client.EchoService.Echo("after_raw_timeout");
        Assert.Equal("after_raw_timeout", result.Message);
        client.Disconnect();
    }

    [Fact]
    public async Task ManyFailedAuthsThenSuccess_ShouldWork()
    {
        // Multiple failed auths
        for (int i = 0; i < 10; i++)
        {
            var failedClient = new E2EClientManager("127.0.0.1", _port);
            try
            {
                await failedClient.ConnectAsync(
                    new TestAuthProto { UserId = 0, Token = "wrong" }, _cts.Token);
            }
            catch (Exception)
            {
                // Expected
            }
        }

        await Task.Delay(500);

        // Should still work after many failures
        var client = new E2EClientManager("127.0.0.1", _port);
        await client.ConnectAsync(new TestAuthProto { UserId = 1, Token = "valid" }, _cts.Token);

        var result = await client.EchoService.Echo("after_many_failures");
        Assert.Equal("after_many_failures", result.Message);
        client.Disconnect();
    }
}
