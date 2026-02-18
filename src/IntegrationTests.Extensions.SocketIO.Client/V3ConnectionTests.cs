using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Ave.Extensions.SocketIO;
using Ave.Extensions.SocketIO.Client;
using Ave.Extensions.SocketIO.Messages;

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

    [Fact(DisplayName = "V3C-007: Connect after disconnect — OnConnected fires twice")]
    public async Task V3C007()
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

    [Theory(DisplayName = "V3C-008: Manual reconnect N times — events fire correct number of times")]
    [InlineData(3)]
    [InlineData(5)]
    public async Task V3C008(int times)
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

    [Theory(DisplayName = "V3C-009: ExtraHeaders passed through to server")]
    [InlineData("x-custom-header", "CustomHeader-Value")]
    [InlineData("user-agent", "dotnet-socketio[client]/socket")]
    public async Task V3C009(string key, string value)
    {
        if (ShouldSkip) return;

        using var client = CreateClient(new SocketIOClientOptions
        {
            EIO = EngineIOVersion.V3,
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
}
