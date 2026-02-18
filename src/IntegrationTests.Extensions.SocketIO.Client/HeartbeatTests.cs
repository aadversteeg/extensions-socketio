using System;
using System.Threading.Tasks;
using FluentAssertions;
using Ave.Extensions.SocketIO;
using Ave.Extensions.SocketIO.Client;

namespace IntegrationTests.Extensions.SocketIO.Client;

[Collection("SocketIO Integration Tests")]
public class HeartbeatTests : IntegrationTestBase
{
    public HeartbeatTests(SocketIOServerFixture fixture) : base(fixture) { }

    [Fact(DisplayName = "IHB-001: OnPing/OnPong events fire during active connection")]
    public async Task IHB001()
    {
        if (ShouldSkip) return;

        using var client = CreateClient();
        var pingFired = new TaskCompletionSource<bool>();
        var pongFired = new TaskCompletionSource<bool>();

        client.OnPing += (_, _) => pingFired.TrySetResult(true);
        client.OnPong += (_, _) => pongFired.TrySetResult(true);

        await client.ConnectAsync();

        // Server has pingInterval=300ms, so heartbeats should occur quickly
        var pingCompleted = await Task.WhenAny(pingFired.Task, Task.Delay(5000));
        pingCompleted.Should().Be(pingFired.Task, "OnPing event should have fired");

        var pongCompleted = await Task.WhenAny(pongFired.Task, Task.Delay(5000));
        pongCompleted.Should().Be(pongFired.Task, "OnPong event should have fired");

        await client.DisconnectAsync();
    }

    [Fact(DisplayName = "IHB-002: Connection stays alive through multiple heartbeat cycles")]
    public async Task IHB002()
    {
        if (ShouldSkip) return;

        using var client = CreateClient();
        var pingCount = 0;

        client.OnPing += (_, _) => pingCount++;

        await client.ConnectAsync();

        // Server has pingInterval=300ms; wait for at least 3 heartbeat cycles
        await Task.Delay(2000);

        client.Connected.Should().BeTrue();
        pingCount.Should().BeGreaterThanOrEqualTo(3,
            "multiple heartbeat cycles should have occurred with pingInterval=300ms");

        // Verify the connection still works
        var echoReceived = new TaskCompletionSource<string?>();
        client.On("message-back", ctx =>
        {
            echoReceived.TrySetResult(ctx.GetValue<string>(0));
            return Task.CompletedTask;
        });

        await client.EmitAsync("message", new object[] { "heartbeat-test" });

        var completed = await Task.WhenAny(echoReceived.Task, Task.Delay(5000));
        completed.Should().Be(echoReceived.Task, "connection should still work after heartbeat cycles");

        var value = await echoReceived.Task;
        value.Should().Be("heartbeat-test");

        await client.DisconnectAsync();
    }
}
