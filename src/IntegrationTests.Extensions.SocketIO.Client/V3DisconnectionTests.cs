using System;
using System.Threading.Tasks;
using FluentAssertions;
using Ave.Extensions.SocketIO;
using Ave.Extensions.SocketIO.Client;

namespace IntegrationTests.Extensions.SocketIO.Client;

[Collection("SocketIO V3 Integration Tests")]
public class V3DisconnectionTests : V3IntegrationTestBase
{
    public V3DisconnectionTests(SocketIOV2ServerFixture fixture) : base(fixture) { }

    [Fact(DisplayName = "V3D-001: Client disconnect via DisconnectAsync — OnDisconnected fires with 'io client disconnect'")]
    public async Task V3D001()
    {
        if (ShouldSkip) return;

        using var client = CreateClient();
        var disconnectReason = new TaskCompletionSource<string>();

        client.OnDisconnected += (_, reason) => disconnectReason.TrySetResult(reason);

        await client.ConnectAsync();
        await client.DisconnectAsync();

        var completed = await Task.WhenAny(disconnectReason.Task, Task.Delay(5000));
        completed.Should().Be(disconnectReason.Task, "OnDisconnected event should have fired");

        var reason = await disconnectReason.Task;
        reason.Should().Be(DisconnectReason.IOClientDisconnect);
    }

    [Fact(DisplayName = "V3D-002: Connected is false after disconnect")]
    public async Task V3D002()
    {
        if (ShouldSkip) return;

        using var client = CreateClient();
        await client.ConnectAsync();

        client.Connected.Should().BeTrue();

        await client.DisconnectAsync();

        client.Connected.Should().BeFalse();
    }

    [Fact(DisplayName = "V3D-003: Server disconnect — OnDisconnected fires with 'io server disconnect'")]
    public async Task V3D003()
    {
        if (ShouldSkip) return;

        using var client = CreateClient();
        var disconnectReason = new TaskCompletionSource<string>();

        client.OnDisconnected += (_, reason) => disconnectReason.TrySetResult(reason);

        await client.ConnectAsync();

        // Ask the server to forcefully disconnect this client
        await client.EmitAsync("force-disconnect", Array.Empty<object>());

        var completed = await Task.WhenAny(disconnectReason.Task, Task.Delay(5000));
        completed.Should().Be(disconnectReason.Task, "OnDisconnected event should have fired");

        var reason = await disconnectReason.Task;
        reason.Should().Be(DisconnectReason.IOServerDisconnect);
    }
}
