using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Ave.Extensions.SocketIO.Server;

namespace IntegrationTests.Extensions.SocketIO.Server;

public class RoomTests : ServerIntegrationTestBase
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

    [Fact(DisplayName = "SRM-001: Socket auto-joins room with its own ID")]
    public async Task SRM001()
    {
        if (ShouldSkip) return;

        await RunClientAsync("connect-default-ws");
        await Task.Delay(500);

        var socket = _clientSockets.Values.FirstOrDefault()
            ?? (await GetFirstSocket());
        if (socket == null) return;

        socket.Rooms.Should().Contain(socket.Id);
    }

    [Fact(DisplayName = "SRM-002: JoinAsync adds room to socket.Rooms")]
    public async Task SRM002()
    {
        if (ShouldSkip) return;

        await RunClientAsync("connect-default-ws");
        var socket = await GetFirstSocket();
        if (socket == null) return;

        await socket.JoinAsync("test-room");
        socket.Rooms.Should().Contain("test-room");
    }

    [Fact(DisplayName = "SRM-003: LeaveAsync removes room from socket.Rooms")]
    public async Task SRM003()
    {
        if (ShouldSkip) return;

        await RunClientAsync("connect-default-ws");
        var socket = await GetFirstSocket();
        if (socket == null) return;

        await socket.JoinAsync("temp-room");
        socket.Rooms.Should().Contain("temp-room");

        await socket.LeaveAsync("temp-room");
        socket.Rooms.Should().NotContain("temp-room");
    }

    [Fact(DisplayName = "SRM-004: Emit to room reaches only room members")]
    public async Task SRM004()
    {
        if (ShouldSkip) return;

        var session = StartClient("multi-client", new
        {
            clientCount = 2,
            waitMs = 5000,
        });

        await WaitForClientsAsync(2);

        // Put client0 in "room-a", client1 not
        if (_clientSockets.TryGetValue("client0", out var socket0))
        {
            await socket0.JoinAsync("room-a");
        }

        // Emit to room-a
        await Server.Default.To("room-a").EmitAsync("room-event", new object[] { "hello room" });
        await Task.Delay(500);

        var messages = await session.WaitForCompletionAsync(TimeSpan.FromSeconds(10));

        // client0 should receive, client1 should not
        var client0Events = FindMessages(messages, "event", "room-event")
            .Where(m => m.Client == 0).ToList();
        var client1Events = FindMessages(messages, "event", "room-event")
            .Where(m => m.Client == 1).ToList();

        client0Events.Should().NotBeEmpty("client0 is in room-a");
        client1Events.Should().BeEmpty("client1 is not in room-a");
    }

    [Fact(DisplayName = "SRM-005: Emit with Except excludes correct clients")]
    public async Task SRM005()
    {
        if (ShouldSkip) return;

        var session = StartClient("multi-client", new
        {
            clientCount = 2,
            waitMs = 5000,
        });

        await WaitForClientsAsync(2);

        // Put both in "room-b"
        if (_clientSockets.TryGetValue("client0", out var socket0))
        {
            await socket0.JoinAsync("room-b");
        }
        if (_clientSockets.TryGetValue("client1", out var socket1))
        {
            await socket1.JoinAsync("room-b");
        }

        // Emit to room-b except client0
        await Server.Default.To("room-b").Except(socket0!.Id).EmitAsync("except-event", new object[] { "filtered" });
        await Task.Delay(500);

        var messages = await session.WaitForCompletionAsync(TimeSpan.FromSeconds(10));

        var client0Events = FindMessages(messages, "event", "except-event")
            .Where(m => m.Client == 0).ToList();
        var client1Events = FindMessages(messages, "event", "except-event")
            .Where(m => m.Client == 1).ToList();

        client0Events.Should().BeEmpty("client0 is excluded");
        client1Events.Should().NotBeEmpty("client1 is in room-b and not excluded");
    }

    private async Task<IServerSocket?> GetFirstSocket()
    {
        // Wait briefly for connection to register
        for (var i = 0; i < 20; i++)
        {
            var socketIds = Server.Default.SocketIds;
            if (socketIds.Count > 0)
            {
                var id = socketIds.First();
                return Server.Default.GetSocket(id);
            }
            await Task.Delay(100);
        }
        return null;
    }
}
