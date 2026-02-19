using System;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Ave.Extensions.SocketIO.Server;

namespace IntegrationTests.Extensions.SocketIO.Server;

public class AcknowledgementTests : ServerIntegrationTestBase
{
    private readonly TaskCompletionSource<string?> _serverAckReceived = new();

    protected override void ConfigureServer(ISocketIOServer server)
    {
        server.OnConnection(socket =>
        {
            // Client emits with ack, server responds
            socket.On("message-with-ack", async ctx =>
            {
                var value = ctx.GetValue<string>(0);
                await ctx.SendAckDataAsync(new object[] { "ack:" + value });
            });

            // Server emits with ack, client responds
            socket.On("ready", async ctx =>
            {
                await socket.EmitAsync("ask-client", new object[] { "question" }, ackMsg =>
                {
                    var value = ackMsg.GetValue<string>(0);
                    _serverAckReceived.TrySetResult(value);
                    return Task.CompletedTask;
                });
            });

            // Complex data ack
            socket.On("complex-ack", async ctx =>
            {
                var value = ctx.GetValue<JsonElement>(0);
                await ctx.SendAckDataAsync(new object[] { value });
            });

            return Task.CompletedTask;
        });
    }

    [Fact(DisplayName = "SAK-001: Client emits with ack, server responds via SendAckDataAsync")]
    public async Task SAK001()
    {
        if (ShouldSkip) return;

        var messages = await RunClientAsync("emit-with-ack", new
        {
            @event = "message-with-ack",
            data = new[] { "hello" },
        });

        var ack = FindMessage(messages, "ack");
        ack.Should().NotBeNull();
        ack!.Data.Should().NotBeNull();
        ack.Data!.Value.ToString().Should().Contain("ack:hello");
    }

    [Fact(DisplayName = "SAK-002: Server emits with ack, client responds")]
    public async Task SAK002()
    {
        if (ShouldSkip) return;

        await RunClientAsync("receive-ack", new
        {
            @event = "ask-client",
            ackResponse = new[] { "answer" },
            waitMs = 2000,
        });

        var result = await WaitForAsync(_serverAckReceived, 5000);
        result.Should().Be("answer");
    }

    [Fact(DisplayName = "SAK-003: Complex object round-trip via ack")]
    public async Task SAK003()
    {
        if (ShouldSkip) return;

        var messages = await RunClientAsync("emit-with-ack", new
        {
            @event = "complex-ack",
            data = new object[] { new { name = "test", value = 42 } },
        });

        var ack = FindMessage(messages, "ack");
        ack.Should().NotBeNull();
        ack!.Data.Should().NotBeNull();
        ack.Data!.Value.ToString().Should().Contain("test");
        ack.Data!.Value.ToString().Should().Contain("42");
    }
}
