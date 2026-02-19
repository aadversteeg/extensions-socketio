using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Ave.Extensions.SocketIO.Server;

namespace IntegrationTests.Extensions.SocketIO.Server;

public class BroadcastTests : ServerIntegrationTestBase
{
    private readonly ConcurrentDictionary<string, IServerSocket> _clientSockets = new();

    protected override void ConfigureServer(ISocketIOServer server)
    {
        server.OnConnection(socket =>
        {
            socket.On("ready", async ctx =>
            {
                var clientName = ctx.GetValue<string>(0);
                if (clientName != null)
                {
                    _clientSockets[clientName] = socket;
                }
            });

            return Task.CompletedTask;
        });
    }

    private async Task WaitForClientsAsync(int count, int timeoutMs = 5000)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromMilliseconds(timeoutMs);
        while (_clientSockets.Count < count && DateTime.UtcNow < deadline)
        {
            await Task.Delay(100);
        }
        _clientSockets.Count.Should().BeGreaterThanOrEqualTo(count,
            $"Expected {count} clients ready but only {_clientSockets.Count} registered");
    }

    [Fact(DisplayName = "SBC-001: Server EmitAsync reaches all connected clients")]
    public async Task SBC001()
    {
        if (ShouldSkip) return;

        var session = StartClient("multi-client", new
        {
            clientCount = 2,
            waitMs = 5000,
        });

        await WaitForClientsAsync(2);

        // Emit to all in default namespace
        await Server.EmitAsync("broadcast-all", new object[] { "hello everyone" });
        await Task.Delay(500);

        var messages = await session.WaitForCompletionAsync(TimeSpan.FromSeconds(10));

        var client0Events = FindMessages(messages, "event", "broadcast-all")
            .Where(m => m.Client == 0).ToList();
        var client1Events = FindMessages(messages, "event", "broadcast-all")
            .Where(m => m.Client == 1).ToList();

        client0Events.Should().NotBeEmpty("client0 should receive broadcast");
        client1Events.Should().NotBeEmpty("client1 should receive broadcast");
    }

    [Fact(DisplayName = "SBC-002: socket.Broadcast.EmitAsync reaches all except sender")]
    public async Task SBC002()
    {
        if (ShouldSkip) return;

        var session = StartClient("multi-client", new
        {
            clientCount = 2,
            waitMs = 5000,
        });

        await WaitForClientsAsync(2);

        // Broadcast from client0's socket (should not reach client0)
        if (_clientSockets.TryGetValue("client0", out var socket0))
        {
            await socket0.Broadcast.EmitAsync("broadcast-others", new object[] { "not for sender" });
        }
        await Task.Delay(500);

        var messages = await session.WaitForCompletionAsync(TimeSpan.FromSeconds(10));

        var client0Events = FindMessages(messages, "event", "broadcast-others")
            .Where(m => m.Client == 0).ToList();
        var client1Events = FindMessages(messages, "event", "broadcast-others")
            .Where(m => m.Client == 1).ToList();

        client0Events.Should().BeEmpty("sender should not receive broadcast");
        client1Events.Should().NotBeEmpty("other client should receive broadcast");
    }

    [Fact(DisplayName = "SBC-003: socket.To(room).EmitAsync targets specific room only")]
    public async Task SBC003()
    {
        if (ShouldSkip) return;

        var session = StartClient("multi-client", new
        {
            clientCount = 2,
            waitMs = 5000,
        });

        await WaitForClientsAsync(2);

        // Put client1 in "vip-room"
        if (_clientSockets.TryGetValue("client1", out var socket1))
        {
            await socket1.JoinAsync("vip-room");
        }

        // Emit to "vip-room" only from client0
        if (_clientSockets.TryGetValue("client0", out var socket0))
        {
            await socket0.To("vip-room").EmitAsync("vip-event", new object[] { "vip message" });
        }
        await Task.Delay(500);

        var messages = await session.WaitForCompletionAsync(TimeSpan.FromSeconds(10));

        var client0Events = FindMessages(messages, "event", "vip-event")
            .Where(m => m.Client == 0).ToList();
        var client1Events = FindMessages(messages, "event", "vip-event")
            .Where(m => m.Client == 1).ToList();

        client0Events.Should().BeEmpty("client0 is not in vip-room");
        client1Events.Should().NotBeEmpty("client1 is in vip-room");
    }
}
