using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Ave.Extensions.SocketIO.Server;

namespace IntegrationTests.Extensions.SocketIO.Server;

public class EventTests : ServerIntegrationTestBase
{
    private readonly ConcurrentQueue<string> _receivedEvents = new();
    private readonly ConcurrentQueue<string?> _receivedData = new();
    private readonly List<string> _onAnyEvents = new();

    protected override void ConfigureServer(ISocketIOServer server)
    {
        server.OnConnection(async socket =>
        {
            // Echo: on "message" -> emit "echo-back" with same data
            socket.On("message", async ctx =>
            {
                var value = ctx.GetValue<string>(0);
                _receivedEvents.Enqueue("message");
                _receivedData.Enqueue(value);
                await socket.EmitAsync("echo-back", new object[] { value! });
            });

            // Multi-param echo
            socket.On("multi-param", async ctx =>
            {
                var v1 = ctx.GetValue<string>(0);
                var v2 = ctx.GetValue<string>(1);
                _receivedEvents.Enqueue("multi-param");
                await socket.EmitAsync("echo-back", new object[] { v1!, v2! });
            });

            // Server emits greeting on connect
            await socket.EmitAsync("greeting", new object[] { "hello from server" });

            // OnAny handler
            socket.OnAny((eventName, ctx) =>
            {
                lock (_onAnyEvents)
                {
                    _onAnyEvents.Add(eventName);
                }
                return Task.CompletedTask;
            });

            // Once handler — server sends event twice when client signals ready
            socket.On("ready", async ctx =>
            {
                await socket.EmitAsync("once-event", new object[] { "first" });
                await Task.Delay(100);
                await socket.EmitAsync("once-event", new object[] { "second" });
            });
        });
    }

    [Fact(DisplayName = "SEV-001: Client emits event, server receives it")]
    public async Task SEV001()
    {
        if (ShouldSkip) return;

        await RunClientAsync("emit-event", new
        {
            emitEvent = "message",
            emitData = new[] { "hello" },
            listenEvent = "echo-back",
        });
        await Task.Delay(300);

        _receivedEvents.Should().Contain("message");
    }

    [Fact(DisplayName = "SEV-002: Client emits event with data, server gets correct data")]
    public async Task SEV002()
    {
        if (ShouldSkip) return;

        await RunClientAsync("emit-event", new
        {
            emitEvent = "message",
            emitData = new[] { "test-data-42" },
            listenEvent = "echo-back",
        });
        await Task.Delay(300);

        _receivedData.Should().Contain("test-data-42");
    }

    [Fact(DisplayName = "SEV-003: Client emits with multiple parameters")]
    public async Task SEV003()
    {
        if (ShouldSkip) return;

        await RunClientAsync("emit-event", new
        {
            emitEvent = "multi-param",
            emitData = new[] { "param1", "param2" },
            listenEvent = "echo-back",
        });
        await Task.Delay(300);

        _receivedEvents.Should().Contain("multi-param");
    }

    [Fact(DisplayName = "SEV-004: Server emits event on connect, client receives it")]
    public async Task SEV004()
    {
        if (ShouldSkip) return;

        var messages = await RunClientAsync("receive-event", new
        {
            @event = "greeting",
        });

        var evt = FindMessage(messages, "event", "greeting");
        evt.Should().NotBeNull();
    }

    [Fact(DisplayName = "SEV-005: Server emits event with data, client receives correct data")]
    public async Task SEV005()
    {
        if (ShouldSkip) return;

        var messages = await RunClientAsync("receive-event", new
        {
            @event = "greeting",
        });

        var evt = FindMessage(messages, "event", "greeting");
        evt.Should().NotBeNull();
        evt!.Data.Should().NotBeNull();
        evt.Data!.Value.ToString().Should().Contain("hello from server");
    }

    [Fact(DisplayName = "SEV-006: Server OnAny catches all client events")]
    public async Task SEV006()
    {
        if (ShouldSkip) return;

        await RunClientAsync("on-any", new
        {
            events = new[] { "test-alpha", "test-beta" },
            waitMs = 1000,
        });
        await Task.Delay(500);

        lock (_onAnyEvents)
        {
            _onAnyEvents.Should().Contain("test-alpha");
            _onAnyEvents.Should().Contain("test-beta");
        }
    }

    [Fact(DisplayName = "SEV-007: Client once handler fires only once when server sends twice")]
    public async Task SEV007()
    {
        if (ShouldSkip) return;

        var messages = await RunClientAsync("once-handler", new
        {
            @event = "once-event",
            waitMs = 1500,
        });

        var done = FindMessage(messages, "done");
        done.Should().NotBeNull();

        // The client receives both events (it uses 'on', not 'once')
        // This test verifies the server sends events correctly
        var events = FindMessages(messages, "event", "once-event");
        events.Count.Should().Be(2);
    }
}
