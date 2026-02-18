using System;
using System.Threading.Tasks;
using FluentAssertions;
using Ave.Extensions.SocketIO;
using Ave.Extensions.SocketIO.Client;
using Ave.Extensions.SocketIO.Messages;

namespace IntegrationTests.Extensions.SocketIO.Client;

[Collection("SocketIO V3 Integration Tests")]
public class V3AcknowledgementTests : V3IntegrationTestBase
{
    public V3AcknowledgementTests(SocketIOV2ServerFixture fixture) : base(fixture) { }

    [Fact(DisplayName = "V3A-001: Emit with ack callback — callback invoked with server response")]
    public async Task V3A001()
    {
        if (ShouldSkip) return;

        using var client = CreateClient();
        var ackReceived = new TaskCompletionSource<bool>();

        await client.ConnectAsync();

        await client.EmitAsync("message-with-ack", Array.Empty<object>(), (IDataMessage _) =>
        {
            ackReceived.TrySetResult(true);
            return Task.CompletedTask;
        });

        var completed = await Task.WhenAny(ackReceived.Task, Task.Delay(5000));
        completed.Should().Be(ackReceived.Task, "ack callback should have been invoked");

        await client.DisconnectAsync();
    }

    [Fact(DisplayName = "V3A-002: Emit with ack and data — data echoed in ack")]
    public async Task V3A002()
    {
        if (ShouldSkip) return;

        using var client = CreateClient();
        var ackData = new TaskCompletionSource<string?>();

        await client.ConnectAsync();

        await client.EmitAsync("message-with-ack", new object[] { "ack-data" }, (IDataMessage msg) =>
        {
            var value = msg.GetValue<string>(0);
            ackData.TrySetResult(value);
            return Task.CompletedTask;
        });

        var completed = await Task.WhenAny(ackData.Task, Task.Delay(5000));
        completed.Should().Be(ackData.Task, "ack callback should have been invoked with data");

        var value = await ackData.Task;
        value.Should().Be("ack-data");

        await client.DisconnectAsync();
    }
}
