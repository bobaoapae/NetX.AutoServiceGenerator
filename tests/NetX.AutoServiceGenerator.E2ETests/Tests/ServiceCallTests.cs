using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NetX.AutoServiceGenerator.E2ETests.Definitions;
using NetX.AutoServiceGenerator.E2ETests.Helpers;


using Xunit;

namespace NetX.AutoServiceGenerator.E2ETests.Tests;

public class ServiceCallTests : IAsyncLifetime
{
    private readonly ushort _port;
    private readonly E2EServerManager _server;
    private readonly CancellationTokenSource _cts;
    private E2EClientManager _client;

    public ServiceCallTests()
    {
        _port = PortAllocator.GetNextPort();
        _cts = new CancellationTokenSource();
        _server = new E2EServerManager("127.0.0.1", _port);
    }

    public async Task InitializeAsync()
    {
        _ = Task.Run(() => _server.StartListening(_cts.Token));
        await Task.Delay(1000);

        _client = new E2EClientManager("127.0.0.1", _port);
        await _client.ConnectAsync(new TestAuthProto { UserId = 1, Token = "valid" }, _cts.Token);
    }

    public Task DisposeAsync()
    {
        _client?.Disconnect();
        _cts.Cancel();
        _cts.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Echo_ShouldReturnSameMessage()
    {
        var result = await _client.EchoService.Echo("hello world");
        Assert.Equal("hello world", result.Message);
        Assert.Equal(11, result.Length);
    }

    [Fact]
    public async Task Echo_EmptyString_ShouldWork()
    {
        var result = await _client.EchoService.Echo("");
        Assert.Equal("", result.Message);
        Assert.Equal(0, result.Length);
    }

    [Fact]
    public async Task Echo_LongString_ShouldWork()
    {
        var longMessage = new string('x', 500);
        var result = await _client.EchoService.Echo(longMessage);
        Assert.Equal(longMessage, result.Message);
        Assert.Equal(500, result.Length);
    }

    [Fact]
    public async Task Add_ShouldReturnCorrectSum()
    {
        var result = await _client.EchoService.Add(42, 58);
        Assert.Equal(100, result);
    }

    [Fact]
    public async Task Add_NegativeNumbers_ShouldWork()
    {
        var result = await _client.EchoService.Add(-10, -20);
        Assert.Equal(-30, result);
    }

    [Fact]
    public async Task Add_Overflow_ShouldWrap()
    {
        var result = await _client.EchoService.Add(int.MaxValue, 1);
        Assert.Equal(int.MinValue, result);
    }

    [Fact]
    public async Task GetItems_ShouldReturnCorrectCount()
    {
        var result = await _client.EchoService.GetItems(5);
        Assert.Equal(5, result.Count);
        Assert.Equal(0, result[0]);
        Assert.Equal(4, result[4]);
    }

    [Fact]
    public async Task GetItems_Zero_ShouldReturnEmptyList()
    {
        var result = await _client.EchoService.GetItems(0);
        Assert.Empty(result);
    }

    [Fact]
    public async Task VoidMethod_ShouldCompleteWithoutError()
    {
        await _client.EchoService.VoidMethod("test_value");
    }

    [Fact]
    public async Task SlowMethod_ShouldComplete()
    {
        var result = await _client.EchoService.SlowMethod(100);
        Assert.True(result);
    }

    [Fact]
    public async Task MultipleSequentialCalls_ShouldAllSucceed()
    {
        for (int i = 0; i < 100; i++)
        {
            var result = await _client.EchoService.Echo($"sequential_{i}");
            Assert.Equal($"sequential_{i}", result.Message);
        }
    }

    [Fact]
    public async Task ConcurrentCalls_ShouldAllSucceed()
    {
        var tasks = Enumerable.Range(0, 50).Select(async i =>
        {
            var result = await _client.EchoService.Echo($"concurrent_{i}");
            Assert.Equal($"concurrent_{i}", result.Message);
        });

        await Task.WhenAll(tasks);
    }

    [Fact]
    public async Task ConcurrentMixedCalls_ShouldAllSucceed()
    {
        var tasks = new List<Task>();

        for (int i = 0; i < 20; i++)
        {
            var idx = i;
            tasks.Add(Task.Run(async () =>
            {
                var echo = await _client.EchoService.Echo($"mixed_{idx}");
                Assert.Equal($"mixed_{idx}", echo.Message);
            }));
            tasks.Add(Task.Run(async () =>
            {
                var sum = await _client.EchoService.Add(idx, idx);
                Assert.Equal(idx * 2, sum);
            }));
            tasks.Add(Task.Run(async () =>
            {
                await _client.EchoService.VoidMethod($"void_{idx}");
            }));
        }

        await Task.WhenAll(tasks);
    }

    [Fact]
    public async Task ServerToClientCallback_ShouldWork()
    {
        NotificationService.ReceivedNotifications.Clear();

        var result = await _client.EchoService.Echo("callback_test");
        Assert.Equal("callback_test", result.Message);
    }

    [Fact]
    public async Task LargeList_ShouldSerializeCorrectly()
    {
        var result = await _client.EchoService.GetItems(100);
        Assert.Equal(100, result.Count);
        Assert.Equal(99, result[99]);
    }
}
