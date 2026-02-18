using System;
using System.Threading.Tasks;
using FluentAssertions;
using Ave.Extensions.SocketIO;
using Ave.Extensions.SocketIO.Client;

namespace IntegrationTests.Extensions.SocketIO.Client;

[Collection("SocketIO Integration Tests")]
public class DisconnectionTests : IntegrationTestBase
{
    public DisconnectionTests(SocketIOServerFixture fixture) : base(fixture) { }

    [Fact(DisplayName = "IDC-001: Client disconnect via DisconnectAsync — OnDisconnected fires with 'io client disconnect'")]
    public async Task IDC001()
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

    [Fact(DisplayName = "IDC-002: Connected is false after disconnect")]
    public async Task IDC002()
    {
        if (ShouldSkip) return;

        using var client = CreateClient();
        await client.ConnectAsync();

        client.Connected.Should().BeTrue();

        await client.DisconnectAsync();

        client.Connected.Should().BeFalse();
    }

    [Fact(DisplayName = "IDC-003: Server disconnect — OnDisconnected fires with 'io server disconnect'")]
    public async Task IDC003()
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
