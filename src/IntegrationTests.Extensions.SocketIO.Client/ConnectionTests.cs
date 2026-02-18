using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using FluentAssertions;
using Ave.Extensions.SocketIO;
using Ave.Extensions.SocketIO.Client;
using Ave.Extensions.SocketIO.Messages;

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

    [Fact(DisplayName = "ICN-008: Connect after disconnect — OnConnected fires twice")]
    public async Task ICN008()
    {
        if (ShouldSkip) return;

        using var client = CreateClient();
        var connectCount = 0;
        client.OnConnected += (_, _) => connectCount++;

        await client.ConnectAsync();
        await client.DisconnectAsync();
        await client.ConnectAsync();

        await Task.Delay(100);

        connectCount.Should().Be(2);

        await client.DisconnectAsync();
    }

    [Theory(DisplayName = "ICN-009: Manual reconnect N times — events fire correct number of times")]
    [InlineData(3)]
    [InlineData(5)]
    public async Task ICN009(int times)
    {
        if (ShouldSkip) return;

        using var client = CreateClient();
        var connectCount = 0;
        var disconnectCount = 0;
        client.OnConnected += (_, _) => connectCount++;
        client.OnDisconnected += (_, _) => disconnectCount++;

        for (var i = 0; i < times; i++)
        {
            await client.ConnectAsync();
            await client.DisconnectAsync();
        }

        await Task.Delay(100);

        connectCount.Should().Be(times);
        disconnectCount.Should().Be(times);
    }

    [Fact(DisplayName = "ICN-010: Auth object retrieved via ack — echoed back correctly")]
    public async Task ICN010()
    {
        if (ShouldSkip) return;

        using var client = CreateClient(new SocketIOClientOptions
        {
            Reconnection = false,
            AutoUpgrade = false,
            Transport = TransportProtocol.WebSocket,
            Auth = new { user = "testuser", password = "testpass" },
        });

        await client.ConnectAsync();

        var authReceived = new TaskCompletionSource<AuthCredentials?>();
        await client.EmitAsync("get_auth", Array.Empty<object>(), (IDataMessage msg) =>
        {
            var auth = msg.GetValue<AuthCredentials>(0);
            authReceived.TrySetResult(auth);
            return Task.CompletedTask;
        });

        var completed = await Task.WhenAny(authReceived.Task, Task.Delay(5000));
        completed.Should().Be(authReceived.Task, "get_auth ack should have been received");

        var auth = await authReceived.Task;
        auth.Should().NotBeNull();
        auth!.User.Should().Be("testuser");
        auth.Password.Should().Be("testpass");

        await client.DisconnectAsync();
    }

    [Theory(DisplayName = "ICN-011: ExtraHeaders passed through to server")]
    [InlineData("x-custom-header", "CustomHeader-Value")]
    [InlineData("user-agent", "dotnet-socketio[client]/socket")]
    public async Task ICN011(string key, string value)
    {
        if (ShouldSkip) return;

        using var client = CreateClient(new SocketIOClientOptions
        {
            Reconnection = false,
            AutoUpgrade = false,
            Transport = TransportProtocol.WebSocket,
            ExtraHeaders = new Dictionary<string, string>
            {
                { key, value },
            },
        });

        await client.ConnectAsync();

        var headerReceived = new TaskCompletionSource<string?>();
        var lowerCaseKey = key.ToLowerInvariant();
        await client.EmitAsync("get_header", new object[] { lowerCaseKey }, (IDataMessage msg) =>
        {
            var headerValue = msg.GetValue<string>(0);
            headerReceived.TrySetResult(headerValue);
            return Task.CompletedTask;
        });

        var completed = await Task.WhenAny(headerReceived.Task, Task.Delay(5000));
        completed.Should().Be(headerReceived.Task, "get_header ack should have been received");

        var actual = await headerReceived.Task;
        actual.Should().Be(value);

        await client.DisconnectAsync();
    }

    private class AuthResponse
    {
        [JsonPropertyName("token")]
        public string? Token { get; set; }
    }

    private class AuthCredentials
    {
        [JsonPropertyName("user")]
        public string? User { get; set; }

        [JsonPropertyName("password")]
        public string? Password { get; set; }
    }
}
