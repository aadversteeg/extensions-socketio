using FluentAssertions;
using Moq;
using Ave.Extensions.SocketIO.Client.Infrastructure;
using Ave.Extensions.SocketIO.Client.Protocol.WebSocket;
using Ave.Extensions.SocketIO.Client.Session.EngineIOAdapter;
using Ave.Extensions.SocketIO.Client.Session.WebSocket.EngineIOAdapter;
using Ave.Extensions.SocketIO.Serialization;

namespace UnitTests.Extensions.SocketIO.Client.Session.WebSocket.EngineIOAdapter;

public class WebSocketEngineIO4AdapterTests
{
    private readonly WebSocketEngineIO4Adapter _sut;

    public WebSocketEngineIO4AdapterTests()
    {
        var mockStopwatch = new Mock<IStopwatch>();
        var mockSerializer = new Mock<ISerializer>();
        var mockWsAdapter = new Mock<IWebSocketAdapter>();

        _sut = new WebSocketEngineIO4Adapter(
            mockStopwatch.Object,
            mockSerializer.Object,
            mockWsAdapter.Object);

        _sut.Options = new EngineIOAdapterOptions();
    }

    [Fact(DisplayName = "WE4-001: WriteProtocolFrame should return bytes unchanged")]
    public void WE4001()
    {
        var data = new byte[] { 1, 2, 3 };

        var result = _sut.WriteProtocolFrame(data);

        result.Should().BeSameAs(data);
    }

    [Fact(DisplayName = "WE4-002: ReadProtocolFrame should return bytes unchanged")]
    public void WE4002()
    {
        var data = new byte[] { 1, 2, 3 };

        var result = _sut.ReadProtocolFrame(data);

        result.Should().BeSameAs(data);
    }
}
