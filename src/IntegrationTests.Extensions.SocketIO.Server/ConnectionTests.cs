using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Ave.Extensions.SocketIO.Server;

namespace IntegrationTests.Extensions.SocketIO.Server;

public class ConnectionTests : ServerIntegrationTestBase
{
    private readonly ConcurrentQueue<IServerSocket> _connectedSockets = new();
    private readonly ConcurrentQueue<IServerSocket> _customNsSockets = new();

    protected override void ConfigureServer(ISocketIOServer server)
    {
        server.OnConnection(socket =>
        {
            _connectedSockets.Enqueue(socket);
            return Task.CompletedTask;
        });

        server.Of("/custom").OnConnection(socket =>
        {
            _customNsSockets.Enqueue(socket);
            return Task.CompletedTask;
        });
    }

    [Fact(DisplayName = "SCN-001: Client connects via WebSocket to default namespace")]
    public async Task SCN001()
    {
        if (ShouldSkip) return;

        // First, verify the server is up by doing our own HTTP request
        using var httpClient = new System.Net.Http.HttpClient();
        var pollResponse = await httpClient.GetStringAsync($"http://localhost:{Port}/socket.io/?EIO=4&transport=polling");
        pollResponse.Should().StartWith("0{", $"Server should return Engine.IO open packet but got: {pollResponse}");

        var messages = await RunClientAsync("connect-default-ws");

        // Diagnostic: dump all messages for debugging
        var allMessages = string.Join("; ", messages.Select(m => $"[{m.Type}:{m.Message ?? m.Id ?? ""}]"));

        var connected = FindMessage(messages, "connected");
        connected.Should().NotBeNull($"Expected 'connected' message but got ({messages.Count} messages): {allMessages}");
        connected!.Id.Should().NotBeNullOrEmpty();

        var done = FindMessage(messages, "done");
        done.Should().NotBeNull();
        done!.Success.Should().BeTrue();
    }

    [Fact(DisplayName = "SCN-002: Client connects via polling to default namespace")]
    public async Task SCN002()
    {
        if (ShouldSkip) return;

        var messages = await RunClientAsync("connect-default-polling");

        var connected = FindMessage(messages, "connected");
        connected.Should().NotBeNull();
        connected!.Id.Should().NotBeNullOrEmpty();
    }

    [Fact(DisplayName = "SCN-003: Client connects to custom namespace")]
    public async Task SCN003()
    {
        if (ShouldSkip) return;

        var messages = await RunClientAsync("connect-custom-ns");

        var connected = FindMessage(messages, "connected");
        connected.Should().NotBeNull();
        connected!.Namespace.Should().Be("/custom");
    }

    [Fact(DisplayName = "SCN-004: Server OnConnection callback fires with valid socket")]
    public async Task SCN004()
    {
        if (ShouldSkip) return;

        await RunClientAsync("connect-default-ws");
        await Task.Delay(500);

        _connectedSockets.Should().NotBeEmpty();
        _connectedSockets.TryPeek(out var socket).Should().BeTrue();
        socket!.Id.Should().NotBeNullOrEmpty();
    }

    [Fact(DisplayName = "SCN-005: Server socket has valid handshake data")]
    public async Task SCN005()
    {
        if (ShouldSkip) return;

        await RunClientAsync("connect-with-query", new { query = new { foo = "bar", test = "123" } });
        await Task.Delay(500);

        _connectedSockets.Should().NotBeEmpty();
        _connectedSockets.TryDequeue(out var socket).Should().BeTrue();
        socket!.Handshake.Should().NotBeNull();
        socket.Handshake.Headers.Should().NotBeNull();
        socket.Handshake.Query.Should().NotBeNull();
        socket.Handshake.Query.Should().ContainKey("foo");
        socket.Handshake.Query!["foo"].Should().Be("bar");
    }
}
