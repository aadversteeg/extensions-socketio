using FluentAssertions;
using Ave.Extensions.SocketIO.Client.Protocol.WebSocket;

namespace UnitTests.Extensions.SocketIO.Client.Protocol.WebSocket;

public class WebSocketMessageTests
{
    [Fact(DisplayName = "WSM-001: Default values should be correct")]
    public void WSM001()
    {
        var message = new WebSocketMessage();

        message.Type.Should().Be(WebSocketMessageType.Text);
    }
}
