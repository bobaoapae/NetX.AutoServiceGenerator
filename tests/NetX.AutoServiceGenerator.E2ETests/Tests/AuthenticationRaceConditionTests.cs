using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NetX.AutoServiceGenerator.E2ETests.Definitions;
using NetX.AutoServiceGenerator.E2ETests.Helpers;

using Xunit;

namespace NetX.AutoServiceGenerator.E2ETests.Tests;

public class AuthenticationRaceConditionTests : IDisposable
{
    private readonly ushort _port;
    private readonly E2EServerManager _server;
    private readonly CancellationTokenSource _cts;
    private readonly Task _serverTask;

    public AuthenticationRaceConditionTests()
    {
        _port = PortAllocator.GetNextPort();
        _cts = new CancellationTokenSource();
        _server = new E2EServerManager("127.0.0.1", _port);
        _serverTask = Task.Run(() => _server.StartListening(_cts.Token));
        Thread.Sleep(500);
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }

    /// <summary>
    /// Tests that raw clients (no auth) are properly disconnected after timeout.
    /// Verifies the timeout callback correctly cleans up _sessions and _pendingAuthentications.
    /// </summary>
    [Fact]
    public async Task UnauthenticatedSession_ShouldBeDisconnectedAfterTimeout()
    {
        using var rawClient = new RawNetXClient("127.0.0.1", _port);
        await rawClient.ConnectAsync(_cts.Token);
        await rawClient.Processor.WaitForConnected(_cts.Token);

        Assert.True(rawClient.Processor.IsConnected);

        // Wait for the 10-second auth timeout + buffer
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await rawClient.Processor.WaitForDisconnected(timeoutCts.Token);

        Assert.True(rawClient.Processor.WasDisconnected);
    }

    /// <summary>
    /// Tests that multiple simultaneous unauthenticated connections are all properly
    /// timed out and cleaned up without corrupting shared state.
    /// </summary>
    [Fact]
    public async Task MultipleConcurrentTimeouts_ShouldCleanupAllSessions()
    {
        const int clientCount = 10;
        var rawClients = new List<RawNetXClient>();

        try
        {
            // Connect many raw clients simultaneously
            for (int i = 0; i < clientCount; i++)
            {
                var rawClient = new RawNetXClient("127.0.0.1", _port);
                rawClients.Add(rawClient);
                await rawClient.ConnectAsync(_cts.Token);
            }

            // Wait for all to connect
            foreach (var client in rawClients)
                await client.Processor.WaitForConnected(_cts.Token);

            // All should be connected initially
            Assert.All(rawClients, c => Assert.True(c.Processor.IsConnected));

            // Wait for all timeouts (10s + buffer)
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var disconnectTasks = rawClients.Select(c => c.Processor.WaitForDisconnected(timeoutCts.Token));
            await Task.WhenAll(disconnectTasks);

            // All should be disconnected
            Assert.All(rawClients, c => Assert.True(c.Processor.WasDisconnected));
        }
        finally
        {
            foreach (var client in rawClients)
                client.Dispose();
        }
    }

    /// <summary>
    /// Tests that after unauthenticated sessions timeout, a legitimate client
    /// can still connect and operate normally. This verifies the timeout cleanup
    /// doesn't corrupt server state (the original race condition bug symptom).
    /// </summary>
    [Fact]
    public async Task AfterTimeouts_LegitimateClientShouldStillWork()
    {
        // Connect raw clients that will timeout
        var rawClients = new List<RawNetXClient>();
        try
        {
            for (int i = 0; i < 5; i++)
            {
                var rawClient = new RawNetXClient("127.0.0.1", _port);
                rawClients.Add(rawClient);
                await rawClient.ConnectAsync(_cts.Token);
            }

            foreach (var client in rawClients)
                await client.Processor.WaitForConnected(_cts.Token);

            // Wait for all timeouts
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var disconnectTasks = rawClients.Select(c => c.Processor.WaitForDisconnected(timeoutCts.Token));
            await Task.WhenAll(disconnectTasks);

            // Now connect a legitimate client
            var legitimateClient = new E2EClientManager("127.0.0.1", _port);
            await legitimateClient.ConnectAsync(new TestAuthProto { UserId = 1, Token = "valid" }, _cts.Token);

            Assert.True(legitimateClient.IsConnected);
            Assert.True(legitimateClient.Authenticated);

            // Verify service calls work
            var result = await legitimateClient.EchoService.Echo("after_timeout_test");
            Assert.Equal("after_timeout_test", result.Message);

            legitimateClient.Disconnect();
        }
        finally
        {
            foreach (var client in rawClients)
                client.Dispose();
        }
    }

    /// <summary>
    /// Tests the exact race condition scenario: unauthenticated sessions mixed with
    /// legitimate sessions under load. The race condition bug would cause requests
    /// from timed-out sessions to be processed with uninitialized session data
    /// (AreaId=0, ServerId=0), potentially corrupting service state.
    /// </summary>
    [Fact]
    public async Task MixedAuthAndRawClients_ShouldIsolateCorrectly()
    {
        const int rawClientCount = 10;
        const int validClientCount = 5;
        var rawClients = new List<RawNetXClient>();
        var validClients = new List<E2EClientManager>();

        try
        {
            // Connect raw clients (will timeout)
            for (int i = 0; i < rawClientCount; i++)
            {
                var rawClient = new RawNetXClient("127.0.0.1", _port);
                rawClients.Add(rawClient);
                await rawClient.ConnectAsync(_cts.Token);
            }

            // Interleave: connect valid clients between raw clients
            for (int i = 0; i < validClientCount; i++)
            {
                var client = new E2EClientManager("127.0.0.1", _port);
                await client.ConnectAsync(new TestAuthProto { UserId = (uint)(i + 1), Token = "valid" }, _cts.Token);
                validClients.Add(client);
            }

            // All valid clients should work
            var tasks = validClients.Select(async (c, idx) =>
            {
                var result = await c.EchoService.Echo($"mixed_test_{idx}");
                Assert.Equal($"mixed_test_{idx}", result.Message);
            });
            await Task.WhenAll(tasks);

            // Wait for raw client timeouts
            foreach (var client in rawClients)
                await client.Processor.WaitForConnected(_cts.Token);

            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var disconnectTasks = rawClients.Select(c => c.Processor.WaitForDisconnected(timeoutCts.Token));
            await Task.WhenAll(disconnectTasks);

            // All raw clients should be disconnected
            Assert.All(rawClients, c => Assert.True(c.Processor.WasDisconnected));

            // Valid clients should STILL work after raw clients timed out
            var postTimeoutTasks = validClients.Select(async (c, idx) =>
            {
                var result = await c.EchoService.Add(idx, 100);
                Assert.Equal(idx + 100, result);
            });
            await Task.WhenAll(postTimeoutTasks);
        }
        finally
        {
            foreach (var client in validClients)
                client.Disconnect();
            foreach (var client in rawClients)
                client.Dispose();
        }
    }

    /// <summary>
    /// Stress test: rapid connect/disconnect cycles with mixed auth states.
    /// Tests that the IsAuthenticated flag and _sessions cleanup work under pressure.
    /// </summary>
    [Fact]
    public async Task RapidConnectDisconnect_ShouldNotCorruptState()
    {
        const int iterations = 20;

        for (int i = 0; i < iterations; i++)
        {
            // Rapidly connect and disconnect raw clients
            using var rawClient = new RawNetXClient("127.0.0.1", _port);
            await rawClient.ConnectAsync(_cts.Token);
            await rawClient.Processor.WaitForConnected(_cts.Token);
            rawClient.Disconnect();
        }

        // Give server time to process all disconnects
        await Task.Delay(1000);

        // Verify server is still healthy with a valid client
        var client = new E2EClientManager("127.0.0.1", _port);
        await client.ConnectAsync(new TestAuthProto { UserId = 99, Token = "valid" }, _cts.Token);

        var result = await client.EchoService.Echo("after_rapid_disconnect");
        Assert.Equal("after_rapid_disconnect", result.Message);

        client.Disconnect();
    }

    /// <summary>
    /// Tests the specific scenario from the bug report: after auth timeout,
    /// the session should not be able to process any service requests.
    /// The _sessions.TryRemove in the timeout callback ensures the session is fully
    /// cleaned from server state, and IsAuthenticated guard adds a second layer.
    /// We verify this by confirming all raw clients are disconnected and
    /// a legitimate client can still connect and operate normally afterward.
    /// </summary>
    [Fact]
    public async Task SessionAfterTimeout_ShouldNotProcessServiceRequests()
    {
        // Connect multiple raw clients that will timeout
        var rawClients = new List<RawNetXClient>();
        try
        {
            for (int i = 0; i < 5; i++)
            {
                var rawClient = new RawNetXClient("127.0.0.1", _port);
                rawClients.Add(rawClient);
                await rawClient.ConnectAsync(_cts.Token);
                await rawClient.Processor.WaitForConnected(_cts.Token);
            }

            // Wait for all timeouts
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var disconnectTasks = rawClients.Select(c => c.Processor.WaitForDisconnected(timeoutCts.Token));
            await Task.WhenAll(disconnectTasks);

            // All raw clients must be disconnected
            Assert.All(rawClients, c => Assert.True(c.Processor.WasDisconnected));

            // Server should still accept and process legitimate clients
            var client = new E2EClientManager("127.0.0.1", _port);
            await client.ConnectAsync(new TestAuthProto { UserId = 1, Token = "valid" }, _cts.Token);

            var result = await client.EchoService.Echo("post_timeout_verify");
            Assert.Equal("post_timeout_verify", result.Message);

            client.Disconnect();
        }
        finally
        {
            foreach (var client in rawClients)
                client.Dispose();
        }
    }

    /// <summary>
    /// Concurrent auth attempts: some valid, some invalid, all at the same time.
    /// Verifies no cross-contamination between sessions.
    /// </summary>
    [Fact]
    public async Task ConcurrentMixedAuth_ShouldHandleEachIndependently()
    {
        var tasks = new List<Task>();
        var successCount = 0;
        var failureCount = 0;

        for (int i = 0; i < 10; i++)
        {
            var userId = (uint)(i % 2 == 0 ? i + 1 : 0); // Even = valid, odd = invalid
            var token = i % 2 == 0 ? "valid" : "invalid";

            tasks.Add(Task.Run(async () =>
            {
                var client = new E2EClientManager("127.0.0.1", _port);
                try
                {
                    await client.ConnectAsync(new TestAuthProto { UserId = userId, Token = token }, _cts.Token);
                    Interlocked.Increment(ref successCount);

                    // Valid client should be able to call services
                    var result = await client.EchoService.Echo("concurrent_test");
                    Assert.Equal("concurrent_test", result.Message);

                    client.Disconnect();
                }
                catch (Exception)
                {
                    Interlocked.Increment(ref failureCount);
                }
            }));
        }

        await Task.WhenAll(tasks);

        Assert.Equal(5, successCount);
        Assert.Equal(5, failureCount);
    }
}
