using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Ave.Extensions.SocketIO;
using Ave.Extensions.SocketIO.Client;

namespace IntegrationTests.Extensions.SocketIO.Client;

[Collection("SocketIO Integration Tests")]
public class EventTests : IntegrationTestBase
{
    public EventTests(SocketIOServerFixture fixture) : base(fixture) { }

    [Fact(DisplayName = "IEV-001: Emit 'message' and receive 'message-back' echo via WebSocket")]
    public async Task IEV001()
    {
        if (ShouldSkip) return;

        using var client = CreateClient();
        var echoReceived = new TaskCompletionSource<string?>();

        client.On("message-back", ctx =>
        {
            var value = ctx.GetValue<string>(0);
            echoReceived.TrySetResult(value);
            return Task.CompletedTask;
        });

        await client.ConnectAsync();
        await client.EmitAsync("message", new object[] { "hello" });

        var completed = await Task.WhenAny(echoReceived.Task, Task.Delay(5000));
        completed.Should().Be(echoReceived.Task, "message-back event should have been received");

        var value = await echoReceived.Task;
        value.Should().Be("hello");

        await client.DisconnectAsync();
    }

    [Fact(DisplayName = "IEV-002: Emit 'message' and receive 'message-back' echo via HTTP polling")]
    public async Task IEV002()
    {
        if (ShouldSkip) return;

        using var client = CreatePollingClient();
        var echoReceived = new TaskCompletionSource<string?>();

        client.On("message-back", ctx =>
        {
            var value = ctx.GetValue<string>(0);
            echoReceived.TrySetResult(value);
            return Task.CompletedTask;
        });

        await client.ConnectAsync();
        await client.EmitAsync("message", new object[] { "hello-polling" });

        var completed = await Task.WhenAny(echoReceived.Task, Task.Delay(5000));
        completed.Should().Be(echoReceived.Task, "message-back event should have been received");

        var value = await echoReceived.Task;
        value.Should().Be("hello-polling");

        await client.DisconnectAsync();
    }

    [Fact(DisplayName = "IEV-003: Emit with multiple arguments — all echoed back")]
    public async Task IEV003()
    {
        if (ShouldSkip) return;

        using var client = CreateClient();
        var echoReceived = new TaskCompletionSource<(string?, string?)>();

        client.On("message-back", ctx =>
        {
            var first = ctx.GetValue<string>(0);
            var second = ctx.GetValue<string>(1);
            echoReceived.TrySetResult((first, second));
            return Task.CompletedTask;
        });

        await client.ConnectAsync();
        await client.EmitAsync("message", new object[] { "arg1", "arg2" });

        var completed = await Task.WhenAny(echoReceived.Task, Task.Delay(5000));
        completed.Should().Be(echoReceived.Task, "message-back event should have been received");

        var (val1, val2) = await echoReceived.Task;
        val1.Should().Be("arg1");
        val2.Should().Be("arg2");

        await client.DisconnectAsync();
    }

    [Fact(DisplayName = "IEV-004: Emit with object argument — deserialized correctly")]
    public async Task IEV004()
    {
        if (ShouldSkip) return;

        using var client = CreateClient();
        var echoReceived = new TaskCompletionSource<TestPayload?>();

        client.On("message-back", ctx =>
        {
            var payload = ctx.GetValue<TestPayload>(0);
            echoReceived.TrySetResult(payload);
            return Task.CompletedTask;
        });

        await client.ConnectAsync();
        await client.EmitAsync("message", new object[] { new TestPayload { Name = "test", Value = 42 } });

        var completed = await Task.WhenAny(echoReceived.Task, Task.Delay(5000));
        completed.Should().Be(echoReceived.Task, "message-back event should have been received");

        var payload = await echoReceived.Task;
        payload.Should().NotBeNull();
        payload!.Name.Should().Be("test");
        payload.Value.Should().Be(42);

        await client.DisconnectAsync();
    }

    [Fact(DisplayName = "IEV-005: On handler receives event data")]
    public async Task IEV005()
    {
        if (ShouldSkip) return;

        using var client = CreateClient();
        var dataReceived = new TaskCompletionSource<string?>();

        client.On("message-back", ctx =>
        {
            var value = ctx.GetValue<string>(0);
            dataReceived.TrySetResult(value);
            return Task.CompletedTask;
        });

        await client.ConnectAsync();
        await client.EmitAsync("message", new object[] { "event-data" });

        var completed = await Task.WhenAny(dataReceived.Task, Task.Delay(5000));
        completed.Should().Be(dataReceived.Task, "On handler should have received the data");

        var value = await dataReceived.Task;
        value.Should().Be("event-data");

        await client.DisconnectAsync();
    }

    [Fact(DisplayName = "IEV-006: Once handler fires only once")]
    public async Task IEV006()
    {
        if (ShouldSkip) return;

        using var client = CreateClient();
        var callCount = 0;
        var firstReceived = new TaskCompletionSource<bool>();

        client.Once("message-back", _ =>
        {
            callCount++;
            firstReceived.TrySetResult(true);
            return Task.CompletedTask;
        });

        await client.ConnectAsync();

        // Send first message
        await client.EmitAsync("message", new object[] { "once-1" });
        await Task.WhenAny(firstReceived.Task, Task.Delay(5000));

        // Send second message
        await client.EmitAsync("message", new object[] { "once-2" });

        // Wait a bit for any potential second invocation
        await Task.Delay(1000);

        callCount.Should().Be(1);

        await client.DisconnectAsync();
    }

    [Fact(DisplayName = "IEV-007: OnAny handler receives all events")]
    public async Task IEV007()
    {
        if (ShouldSkip) return;

        using var client = CreateClient();
        var receivedEvents = new List<string>();
        var messageBackReceived = new TaskCompletionSource<bool>();

        client.OnAny((eventName, _) =>
        {
            receivedEvents.Add(eventName);
            if (eventName == "message-back")
            {
                messageBackReceived.TrySetResult(true);
            }
            return Task.CompletedTask;
        });

        await client.ConnectAsync();
        await client.EmitAsync("message", new object[] { "any-test" });

        var completed = await Task.WhenAny(messageBackReceived.Task, Task.Delay(5000));
        completed.Should().Be(messageBackReceived.Task, "OnAny handler should have received the event");

        receivedEvents.Should().Contain("message-back");

        await client.DisconnectAsync();
    }

    private class TestPayload
    {
        public string? Name { get; set; }
        public int Value { get; set; }
    }
}
