using System;
using System.Threading.Tasks;
using FluentAssertions;
using Ave.Extensions.SocketIO;
using Ave.Extensions.SocketIO.Client;

namespace IntegrationTests.Extensions.SocketIO.Client;

[Collection("SocketIO V3 Integration Tests")]
public class V3TransportTests : V3IntegrationTestBase
{
    public V3TransportTests(SocketIOV2ServerFixture fixture) : base(fixture) { }

    [Fact(DisplayName = "V3T-001: Auto-upgrade from polling to WebSocket when AutoUpgrade=true")]
    public async Task V3T001()
    {
        if (ShouldSkip) return;

        using var client = CreatePollingClient(new SocketIOClientOptions
        {
            EIO = EngineIOVersion.V3,
            Reconnection = false,
            AutoUpgrade = true,
            Transport = TransportProtocol.Polling,
        });

        await client.ConnectAsync();

        client.Connected.Should().BeTrue();

        // After auto-upgrade, the transport option should have been changed to WebSocket
        client.Options.Transport.Should().Be(TransportProtocol.WebSocket);

        // Verify the connection still works by doing an echo
        var echoReceived = new TaskCompletionSource<string?>();
        client.On("message-back", ctx =>
        {
            echoReceived.TrySetResult(ctx.GetValue<string>(0));
            return Task.CompletedTask;
        });

        await client.EmitAsync("message", new object[] { "upgrade-test" });

        var completed = await Task.WhenAny(echoReceived.Task, Task.Delay(5000));
        completed.Should().Be(echoReceived.Task, "message-back should have been received after upgrade");

        var value = await echoReceived.Task;
        value.Should().Be("upgrade-test");

        await client.DisconnectAsync();
    }

    [Fact(DisplayName = "V3T-002: No upgrade when AutoUpgrade=false, stays on polling")]
    public async Task V3T002()
    {
        if (ShouldSkip) return;

        using var client = CreatePollingClient(new SocketIOClientOptions
        {
            EIO = EngineIOVersion.V3,
            Reconnection = false,
            AutoUpgrade = false,
            Transport = TransportProtocol.Polling,
        });

        await client.ConnectAsync();

        client.Connected.Should().BeTrue();
        client.Options.Transport.Should().Be(TransportProtocol.Polling);

        // Verify the polling connection works
        var echoReceived = new TaskCompletionSource<string?>();
        client.On("message-back", ctx =>
        {
            echoReceived.TrySetResult(ctx.GetValue<string>(0));
            return Task.CompletedTask;
        });

        await client.EmitAsync("message", new object[] { "no-upgrade-test" });

        var completed = await Task.WhenAny(echoReceived.Task, Task.Delay(5000));
        completed.Should().Be(echoReceived.Task, "message-back should have been received via polling");

        var value = await echoReceived.Task;
        value.Should().Be("no-upgrade-test");

        await client.DisconnectAsync();
    }

    [Fact(DisplayName = "V3T-003: Direct WebSocket connection (no upgrade path)")]
    public async Task V3T003()
    {
        if (ShouldSkip) return;

        using var client = CreateClient(new SocketIOClientOptions
        {
            EIO = EngineIOVersion.V3,
            Reconnection = false,
            AutoUpgrade = false,
            Transport = TransportProtocol.WebSocket,
        });

        await client.ConnectAsync();

        client.Connected.Should().BeTrue();
        client.Options.Transport.Should().Be(TransportProtocol.WebSocket);

        // Verify the WebSocket connection works
        var echoReceived = new TaskCompletionSource<string?>();
        client.On("message-back", ctx =>
        {
            echoReceived.TrySetResult(ctx.GetValue<string>(0));
            return Task.CompletedTask;
        });

        await client.EmitAsync("message", new object[] { "direct-ws-test" });

        var completed = await Task.WhenAny(echoReceived.Task, Task.Delay(5000));
        completed.Should().Be(echoReceived.Task, "message-back should have been received via WebSocket");

        var value = await echoReceived.Task;
        value.Should().Be("direct-ws-test");

        await client.DisconnectAsync();
    }
}
