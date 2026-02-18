using System;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Ave.Extensions.SocketIO.Client.Infrastructure;
using Ave.Extensions.SocketIO.Client.Protocol.WebSocket;
using Ave.Extensions.SocketIO.Client.Session.EngineIOAdapter;
using Ave.Extensions.SocketIO.Client.Session.WebSocket.EngineIOAdapter;

namespace UnitTests.Extensions.SocketIO.Client.Session.WebSocket.EngineIOAdapter;

public class WebSocketEngineIO3AdapterTests
{
    private readonly WebSocketEngineIO3Adapter _sut;

    public WebSocketEngineIO3AdapterTests()
    {
        var mockStopwatch = new Mock<IStopwatch>();
        var mockLogger = new Mock<ILogger<WebSocketEngineIO3Adapter>>();
        var mockWsAdapter = new Mock<IWebSocketAdapter>();
        var mockDelay = new Mock<IDelay>();

        _sut = new WebSocketEngineIO3Adapter(
            mockStopwatch.Object,
            mockLogger.Object,
            mockWsAdapter.Object,
            mockDelay.Object);

        _sut.Options = new EngineIOAdapterOptions
        {
            Timeout = TimeSpan.FromSeconds(5),
        };
    }

    [Fact(DisplayName = "WE3-001: WriteProtocolFrame should prepend byte 4")]
    public void WE3001()
    {
        var data = new byte[] { 1, 2, 3 };

        var result = _sut.WriteProtocolFrame(data);

        result.Should().HaveCount(4);
        result[0].Should().Be(4);
        result[1].Should().Be(1);
        result[2].Should().Be(2);
        result[3].Should().Be(3);
    }

    [Fact(DisplayName = "WE3-002: ReadProtocolFrame should strip first byte")]
    public void WE3002()
    {
        var data = new byte[] { 4, 1, 2, 3 };

        var result = _sut.ReadProtocolFrame(data);

        result.Should().HaveCount(3);
        result.Should().BeEquivalentTo(new byte[] { 1, 2, 3 });
    }

    [Fact(DisplayName = "WE3-003: WriteProtocolFrame with empty data should return single byte 4")]
    public void WE3003()
    {
        var data = Array.Empty<byte>();

        var result = _sut.WriteProtocolFrame(data);

        result.Should().HaveCount(1);
        result[0].Should().Be(4);
    }

    [Fact(DisplayName = "WE3-004: ReadProtocolFrame and WriteProtocolFrame should be inverse operations")]
    public void WE3004()
    {
        var original = new byte[] { 10, 20, 30, 40 };

        var written = _sut.WriteProtocolFrame(original);
        var read = _sut.ReadProtocolFrame(written);

        read.Should().BeEquivalentTo(original);
    }
}
