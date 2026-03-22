using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NetX.AutoServiceGenerator.E2ETests.Definitions;
using NetX.AutoServiceGenerator.E2ETests.Helpers;

using Xunit;

namespace NetX.AutoServiceGenerator.E2ETests.Tests;

public class ConcurrentSessionTests : IDisposable
{
    private readonly ushort _port;
    private readonly E2EServerManager _server;
    private readonly CancellationTokenSource _cts;

    public ConcurrentSessionTests()
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
    public async Task MultipleSessions_ShouldEachAuthenticateIndependently()
    {
        const int clientCount = 10;
        var clients = new E2EClientManager[clientCount];

        for (int i = 0; i < clientCount; i++)
        {
            clients[i] = new E2EClientManager("127.0.0.1", _port);
            await clients[i].ConnectAsync(new TestAuthProto { UserId = (uint)(i + 1), Token = "valid" }, _cts.Token);
        }

        // All should be authenticated and able to call services
        var tasks = clients.Select(async (c, idx) =>
        {
            var result = await c.EchoService.Echo($"session_{idx}");
            Assert.Equal($"session_{idx}", result.Message);
        });
        await Task.WhenAll(tasks);

        // All connected on server side
        Assert.True(_server.ConnectedSessions.Count >= clientCount);

        foreach (var client in clients)
            client.Disconnect();
    }

    [Fact]
    public async Task DisconnectOneSession_ShouldNotAffectOthers()
    {
        var clients = new E2EClientManager[3];

        for (int i = 0; i < clients.Length; i++)
        {
            clients[i] = new E2EClientManager("127.0.0.1", _port);
            await clients[i].ConnectAsync(new TestAuthProto { UserId = (uint)(i + 1), Token = "valid" }, _cts.Token);
        }

        // All should work
        for (int i = 0; i < clients.Length; i++)
        {
            var result = await clients[i].EchoService.Echo($"before_disconnect_{i}");
            Assert.Equal($"before_disconnect_{i}", result.Message);
        }

        // Disconnect the middle client
        clients[1].Disconnect();
        await Task.Delay(500);

        // Other clients should still work
        var result0 = await clients[0].EchoService.Echo("after_disconnect_0");
        Assert.Equal("after_disconnect_0", result0.Message);

        var result2 = await clients[2].EchoService.Echo("after_disconnect_2");
        Assert.Equal("after_disconnect_2", result2.Message);

        clients[0].Disconnect();
        clients[2].Disconnect();
    }

    [Fact]
    public async Task ManySessionsConcurrentServiceCalls_ShouldAllSucceed()
    {
        const int clientCount = 10;
        const int callsPerClient = 20;
        var clients = new E2EClientManager[clientCount];

        for (int i = 0; i < clientCount; i++)
        {
            clients[i] = new E2EClientManager("127.0.0.1", _port);
            await clients[i].ConnectAsync(new TestAuthProto { UserId = (uint)(i + 1), Token = "valid" }, _cts.Token);
        }

        // All clients make concurrent calls
        var allTasks = new List<Task>();
        for (int c = 0; c < clientCount; c++)
        {
            var client = clients[c];
            var clientIdx = c;
            for (int m = 0; m < callsPerClient; m++)
            {
                var callIdx = m;
                allTasks.Add(Task.Run(async () =>
                {
                    var result = await client.EchoService.Echo($"c{clientIdx}_m{callIdx}");
                    Assert.Equal($"c{clientIdx}_m{callIdx}", result.Message);
                }));
            }
        }

        await Task.WhenAll(allTasks);

        foreach (var client in clients)
            client.Disconnect();
    }

    [Fact]
    public async Task SessionDisconnect_ShouldFireServerCallback()
    {
        var client = new E2EClientManager("127.0.0.1", _port);
        await client.ConnectAsync(new TestAuthProto { UserId = 1, Token = "valid" }, _cts.Token);

        var connectedBefore = _server.ConnectedSessions.Count;
        client.Disconnect();

        await Task.Delay(1000);

        Assert.True(_server.DisconnectedSessions.Count > 0);
    }

    [Fact]
    public async Task StressTest_ManyConcurrentConnections()
    {
        const int clientCount = 20;
        var clients = new List<E2EClientManager>();

        try
        {
            // Connect all at once
            var connectTasks = Enumerable.Range(0, clientCount).Select(async i =>
            {
                var client = new E2EClientManager("127.0.0.1", _port);
                await client.ConnectAsync(new TestAuthProto { UserId = (uint)(i + 1), Token = "valid" }, _cts.Token);
                lock (clients) clients.Add(client);
            });

            await Task.WhenAll(connectTasks);
            Assert.Equal(clientCount, clients.Count);

            // All should work
            var callTasks = clients.Select(async (c, idx) =>
            {
                var result = await c.EchoService.Add(idx, 1);
                Assert.Equal(idx + 1, result);
            });
            await Task.WhenAll(callTasks);
        }
        finally
        {
            foreach (var client in clients)
                client.Disconnect();
        }
    }

    [Fact]
    public async Task AlternatingConnectDisconnect_ShouldMaintainServerHealth()
    {
        // Simulate churn: connect, use, disconnect in rapid succession
        for (int i = 0; i < 10; i++)
        {
            var client = new E2EClientManager("127.0.0.1", _port);
            await client.ConnectAsync(new TestAuthProto { UserId = (uint)(i + 1), Token = "valid" }, _cts.Token);

            var result = await client.EchoService.Echo($"churn_{i}");
            Assert.Equal($"churn_{i}", result.Message);

            client.Disconnect();
            await Task.Delay(100);
        }

        // Server should still be healthy
        var finalClient = new E2EClientManager("127.0.0.1", _port);
        await finalClient.ConnectAsync(new TestAuthProto { UserId = 99, Token = "valid" }, _cts.Token);
        var finalResult = await finalClient.EchoService.Echo("final_check");
        Assert.Equal("final_check", finalResult.Message);
        finalClient.Disconnect();
    }
}
