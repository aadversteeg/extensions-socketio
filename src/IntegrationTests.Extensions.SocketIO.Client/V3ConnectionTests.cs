using System;
using System.Threading.Tasks;
using FluentAssertions;
using Ave.Extensions.SocketIO;
using Ave.Extensions.SocketIO.Client;

namespace IntegrationTests.Extensions.SocketIO.Client;

[Collection("SocketIO V3 Integration Tests")]
public class V3ConnectionTests : V3IntegrationTestBase
{
    public V3ConnectionTests(SocketIOV2ServerFixture fixture) : base(fixture) { }

    [Fact(DisplayName = "V3C-001: Connect to default namespace via WebSocket with EIO=V3")]
    public async Task V3C001()
    {
        if (ShouldSkip) return;

        using var client = CreateClient();
        await client.ConnectAsync();

        client.Connected.Should().BeTrue();
        await client.DisconnectAsync();
    }

    [Fact(DisplayName = "V3C-002: Connect to default namespace via HTTP polling with EIO=V3")]
    public async Task V3C002()
    {
        if (ShouldSkip) return;

        using var client = CreatePollingClient();
        await client.ConnectAsync();

        client.Connected.Should().BeTrue();
        await client.DisconnectAsync();
    }

    [Fact(DisplayName = "V3C-003: Client receives a session ID from handshake after connecting")]
    public async Task V3C003()
    {
        if (ShouldSkip) return;

        using var client = CreateClient();
        await client.ConnectAsync();

        client.Id.Should().NotBeNullOrEmpty();
        await client.DisconnectAsync();
    }

    [Fact(DisplayName = "V3C-004: Connected property is true after connecting")]
    public async Task V3C004()
    {
        if (ShouldSkip) return;

        using var client = CreateClient();
        client.Connected.Should().BeFalse();

        await client.ConnectAsync();

        client.Connected.Should().BeTrue();
        await client.DisconnectAsync();
    }

    [Fact(DisplayName = "V3C-005: OnConnected event fires on successful connection")]
    public async Task V3C005()
    {
        if (ShouldSkip) return;

        using var client = CreateClient();
        var connectedFired = new TaskCompletionSource<bool>();

        client.OnConnected += (_, _) => connectedFired.TrySetResult(true);

        await client.ConnectAsync();

        var completed = await Task.WhenAny(connectedFired.Task, Task.Delay(5000));
        completed.Should().Be(connectedFired.Task, "OnConnected event should have fired");

        await client.DisconnectAsync();
    }

    [Fact(DisplayName = "V3C-006: Connect to custom namespace /custom")]
    public async Task V3C006()
    {
        if (ShouldSkip) return;

        using var client = CreateClientForNamespace("/custom");
        await client.ConnectAsync();

        client.Connected.Should().BeTrue();
        client.Id.Should().NotBeNullOrEmpty();
        await client.DisconnectAsync();
    }
}
