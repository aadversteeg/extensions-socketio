using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using FluentAssertions;
using Ave.Extensions.SocketIO;
using Ave.Extensions.SocketIO.Client;

namespace IntegrationTests.Extensions.SocketIO.Client;

[Collection("SocketIO Integration Tests")]
public class ConnectionTests : IntegrationTestBase
{
    public ConnectionTests(SocketIOServerFixture fixture) : base(fixture) { }

    [Fact(DisplayName = "ICN-001: Connect to default namespace via WebSocket")]
    public async Task ICN001()
    {
        if (ShouldSkip) return;

        using var client = CreateClient();
        await client.ConnectAsync();

        client.Connected.Should().BeTrue();
        await client.DisconnectAsync();
    }

    [Fact(DisplayName = "ICN-002: Connect to default namespace via HTTP polling")]
    public async Task ICN002()
    {
        if (ShouldSkip) return;

        using var client = CreatePollingClient();
        await client.ConnectAsync();

        client.Connected.Should().BeTrue();
        await client.DisconnectAsync();
    }

    [Fact(DisplayName = "ICN-003: Client receives a session ID after connecting")]
    public async Task ICN003()
    {
        if (ShouldSkip) return;

        using var client = CreateClient();
        await client.ConnectAsync();

        client.Id.Should().NotBeNullOrEmpty();
        await client.DisconnectAsync();
    }

    [Fact(DisplayName = "ICN-004: Connected property is true after connecting")]
    public async Task ICN004()
    {
        if (ShouldSkip) return;

        using var client = CreateClient();
        client.Connected.Should().BeFalse();

        await client.ConnectAsync();

        client.Connected.Should().BeTrue();
        await client.DisconnectAsync();
    }

    [Fact(DisplayName = "ICN-005: OnConnected event fires on successful connection")]
    public async Task ICN005()
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

    [Fact(DisplayName = "ICN-006: Connect to custom namespace /custom")]
    public async Task ICN006()
    {
        if (ShouldSkip) return;

        using var client = CreateClientForNamespace("/custom");
        await client.ConnectAsync();

        client.Connected.Should().BeTrue();
        client.Id.Should().NotBeNullOrEmpty();
        await client.DisconnectAsync();
    }

    [Fact(DisplayName = "ICN-007: Connect with auth credentials — server echoes them back")]
    public async Task ICN007()
    {
        if (ShouldSkip) return;

        var authData = new { token = "test-token-123" };
        using var client = CreateClient(new SocketIOClientOptions
        {
            Reconnection = false,
            AutoUpgrade = false,
            Transport = TransportProtocol.WebSocket,
            Auth = authData,
        });

        var authReceived = new TaskCompletionSource<AuthResponse?>();
        client.On("auth", ctx =>
        {
            var auth = ctx.GetValue<AuthResponse>(0);
            authReceived.TrySetResult(auth);
            return Task.CompletedTask;
        });

        await client.ConnectAsync();

        var completed = await Task.WhenAny(authReceived.Task, Task.Delay(5000));
        completed.Should().Be(authReceived.Task, "auth event should have been received");

        var auth = await authReceived.Task;
        auth.Should().NotBeNull();
        auth!.Token.Should().Be("test-token-123");

        await client.DisconnectAsync();
    }

    private class AuthResponse
    {
        [JsonPropertyName("token")]
        public string? Token { get; set; }
    }
}
