using System;
using System.Threading.Tasks;
using FluentAssertions;
using Ave.Extensions.SocketIO;
using Ave.Extensions.SocketIO.Client;

namespace IntegrationTests.Extensions.SocketIO.Client;

[Collection("SocketIO V3 Integration Tests")]
public class V3EventTests : V3IntegrationTestBase
{
    public V3EventTests(SocketIOV2ServerFixture fixture) : base(fixture) { }

    [Fact(DisplayName = "V3E-001: Emit 'message' and receive 'message-back' echo via WebSocket")]
    public async Task V3E001()
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

    [Fact(DisplayName = "V3E-002: Emit 'message' and receive 'message-back' echo via HTTP polling")]
    public async Task V3E002()
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

    [Fact(DisplayName = "V3E-003: Emit with multiple arguments — all echoed back")]
    public async Task V3E003()
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

    [Fact(DisplayName = "V3E-004: Emit with object argument — deserialized correctly")]
    public async Task V3E004()
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

    [Fact(DisplayName = "V3E-005: Once handler fires only once")]
    public async Task V3E005()
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

    [Fact(DisplayName = "V3E-006: Emit null — receive null back")]
    public async Task V3E006()
    {
        if (ShouldSkip) return;

        using var client = CreateClient();
        var echoReceived = new TaskCompletionSource<IEventContext>();

        client.On("1:emit", ctx =>
        {
            echoReceived.TrySetResult(ctx);
            return Task.CompletedTask;
        });

        await client.ConnectAsync();
        await client.EmitAsync("1:emit", new object[] { null! });

        var completed = await Task.WhenAny(echoReceived.Task, Task.Delay(5000));
        completed.Should().Be(echoReceived.Task, "1:emit event should have been received");

        var ctx = await echoReceived.Task;
        ctx.GetValue<object>(0).Should().BeNull();

        await client.DisconnectAsync();
    }

    [Theory(DisplayName = "V3E-007: Emit single primitive parameter — echoed back correctly")]
    [InlineData(true, "V3E-007a")]
    [InlineData(false, "V3E-007b")]
    [InlineData(-1234567890, "V3E-007c")]
    [InlineData(1234567890, "V3E-007d")]
    [InlineData("hello\n世界\n🌍🌎🌏", "V3E-007e")]
    public async Task V3E007(object data, string _)
    {
        if (ShouldSkip) return;

        using var client = CreateClient();
        var echoReceived = new TaskCompletionSource<IEventContext>();

        client.On("1:emit", ctx =>
        {
            echoReceived.TrySetResult(ctx);
            return Task.CompletedTask;
        });

        await client.ConnectAsync();
        await client.EmitAsync("1:emit", new[] { data });

        var completed = await Task.WhenAny(echoReceived.Task, Task.Delay(5000));
        completed.Should().Be(echoReceived.Task, "1:emit event should have been received");

        var ctx = await echoReceived.Task;
        ctx.GetValue(data.GetType(), 0).Should().BeEquivalentTo(data);

        await client.DisconnectAsync();
    }

    [Theory(DisplayName = "V3E-008: Emit 2 parameters with mixed types — both echoed back")]
    [InlineData(true, false, "V3E-008a")]
    [InlineData(false, 123, "V3E-008b")]
    [InlineData(-1234567890, "test", "V3E-008c")]
    [InlineData("hello\n世界\n🌍🌎🌏", 199, "V3E-008d")]
    public async Task V3E008(object item0, object item1, string _)
    {
        if (ShouldSkip) return;

        using var client = CreateClient();
        var echoReceived = new TaskCompletionSource<IEventContext>();

        client.On("2:emit", ctx =>
        {
            echoReceived.TrySetResult(ctx);
            return Task.CompletedTask;
        });

        await client.ConnectAsync();
        await client.EmitAsync("2:emit", new[] { item0, item1 });

        var completed = await Task.WhenAny(echoReceived.Task, Task.Delay(5000));
        completed.Should().Be(echoReceived.Task, "2:emit event should have been received");

        var ctx = await echoReceived.Task;
        ctx.GetValue(item0.GetType(), 0).Should().BeEquivalentTo(item0);
        ctx.GetValue(item1.GetType(), 1).Should().BeEquivalentTo(item1);

        await client.DisconnectAsync();
    }

    [Fact(DisplayName = "V3E-009: OnAny and On handlers both fire for same event")]
    public async Task V3E009()
    {
        if (ShouldSkip) return;

        using var client = CreateClient();
        var onHandlerCalled = new TaskCompletionSource<bool>();
        var onAnyHandlerCalled = new TaskCompletionSource<bool>();

        client.OnAny((_, _) =>
        {
            onAnyHandlerCalled.TrySetResult(true);
            return Task.CompletedTask;
        });
        client.On("1:emit", _ =>
        {
            onHandlerCalled.TrySetResult(true);
            return Task.CompletedTask;
        });

        await client.ConnectAsync();
        await client.EmitAsync("1:emit", new object[] { "test" });

        var bothCompleted = await Task.WhenAll(
            Task.WhenAny(onHandlerCalled.Task, Task.Delay(5000)),
            Task.WhenAny(onAnyHandlerCalled.Task, Task.Delay(5000)));

        bothCompleted[0].Should().Be(onHandlerCalled.Task, "On handler should have been called");
        bothCompleted[1].Should().Be(onAnyHandlerCalled.Task, "OnAny handler should have been called");

        await client.DisconnectAsync();
    }

    private class TestPayload
    {
        public string? Name { get; set; }
        public int Value { get; set; }
    }
}
