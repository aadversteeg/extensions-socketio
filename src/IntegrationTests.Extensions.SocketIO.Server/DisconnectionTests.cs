using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using FluentAssertions;
using Ave.Extensions.SocketIO.Server;

namespace IntegrationTests.Extensions.SocketIO.Server;

public class DisconnectionTests : ServerIntegrationTestBase
{
    private readonly TaskCompletionSource<string> _disconnectReason = new();
    private readonly ConcurrentQueue<IServerSocket> _connectedSockets = new();

    protected override void ConfigureServer(ISocketIOServer server)
    {
        server.OnConnection(socket =>
        {
            _connectedSockets.Enqueue(socket);

            socket.OnDisconnect(reason =>
            {
                _disconnectReason.TrySetResult(reason);
                return Task.CompletedTask;
            });

            socket.On("trigger-disconnect", async ctx =>
            {
                await socket.DisconnectAsync();
            });

            return Task.CompletedTask;
        });
    }

    [Fact(DisplayName = "SDC-001: Client disconnects — server OnDisconnect fires")]
    public async Task SDC001()
    {
        if (ShouldSkip) return;

        await RunClientAsync("client-disconnect");

        var reason = await WaitForAsync(_disconnectReason, 5000);
        reason.Should().NotBeNullOrEmpty();
    }

    [Fact(DisplayName = "SDC-002: Server-initiated disconnect — client receives disconnect")]
    public async Task SDC002()
    {
        if (ShouldSkip) return;

        var messages = await RunClientAsync("server-disconnect");

        var disconnected = FindMessage(messages, "disconnected");
        disconnected.Should().NotBeNull();
        disconnected!.Reason.Should().NotBeNullOrEmpty();
    }

    [Fact(DisplayName = "SDC-003: After disconnect, socket.Connected is false")]
    public async Task SDC003()
    {
        if (ShouldSkip) return;

        await RunClientAsync("client-disconnect");
        await WaitForAsync(_disconnectReason, 5000);
        await Task.Delay(300);

        _connectedSockets.TryPeek(out var socket).Should().BeTrue();
        socket!.Connected.Should().BeFalse();
    }
}
