using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Ave.Extensions.SocketIO.Client.Protocol.WebSocket;
using Ave.Extensions.SocketIO.Protocol;

namespace UnitTests.Extensions.SocketIO.Client.Protocol.WebSocket;

public class WebSocketAdapterTests : IDisposable
{
    private readonly Mock<IWebSocketClientAdapter> _mockClientAdapter;
    private readonly Mock<ILogger<WebSocketAdapter>> _mockLogger;
    private readonly WebSocketAdapter _sut;

    public WebSocketAdapterTests()
    {
        _mockClientAdapter = new Mock<IWebSocketClientAdapter>();
        _mockLogger = new Mock<ILogger<WebSocketAdapter>>();
        _sut = new WebSocketAdapter(_mockLogger.Object, _mockClientAdapter.Object);
    }

    public void Dispose()
    {
        _sut.Dispose();
    }

    [Fact(DisplayName = "WSA-001: SendAsync with text message should encode to UTF8 and send as Text")]
    public async Task WSA001()
    {
        var message = new ProtocolMessage
        {
            Type = ProtocolMessageType.Text,
            Text = "hello"
        };

        byte[]? capturedData = null;
        WebSocketMessageType capturedType = default;
        _mockClientAdapter.Setup(c => c.SendAsync(It.IsAny<byte[]>(), It.IsAny<WebSocketMessageType>(), It.IsAny<CancellationToken>()))
            .Callback<byte[], WebSocketMessageType, CancellationToken>((data, type, _) =>
            {
                capturedData = data;
                capturedType = type;
            })
            .Returns(Task.CompletedTask);

        await _sut.SendAsync(message, CancellationToken.None);

        capturedData.Should().NotBeNull();
        Encoding.UTF8.GetString(capturedData!).Should().Be("hello");
        capturedType.Should().Be(WebSocketMessageType.Text);
    }

    [Fact(DisplayName = "WSA-002: SendAsync with binary message should send as Binary")]
    public async Task WSA002()
    {
        var bytes = new byte[] { 1, 2, 3 };
        var message = new ProtocolMessage
        {
            Type = ProtocolMessageType.Bytes,
            Bytes = bytes
        };

        byte[]? capturedData = null;
        WebSocketMessageType capturedType = default;
        _mockClientAdapter.Setup(c => c.SendAsync(It.IsAny<byte[]>(), It.IsAny<WebSocketMessageType>(), It.IsAny<CancellationToken>()))
            .Callback<byte[], WebSocketMessageType, CancellationToken>((data, type, _) =>
            {
                capturedData = data;
                capturedType = type;
            })
            .Returns(Task.CompletedTask);

        await _sut.SendAsync(message, CancellationToken.None);

        capturedData.Should().BeEquivalentTo(bytes);
        capturedType.Should().Be(WebSocketMessageType.Binary);
    }

    [Fact(DisplayName = "WSA-003: SendAsync with text message where Text is null should throw ArgumentNullException")]
    public async Task WSA003()
    {
        var message = new ProtocolMessage
        {
            Type = ProtocolMessageType.Text,
            Text = null
        };

        var act = () => _sut.SendAsync(message, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact(DisplayName = "WSA-004: SendAsync with bytes message where Bytes is null should throw ArgumentNullException")]
    public async Task WSA004()
    {
        var message = new ProtocolMessage
        {
            Type = ProtocolMessageType.Bytes,
            Bytes = null
        };

        var act = () => _sut.SendAsync(message, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact(DisplayName = "WSA-005: ConnectAsync should delegate to client adapter")]
    public async Task WSA005()
    {
        var uri = new Uri("ws://localhost");
        _mockClientAdapter.Setup(c => c.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockClientAdapter.Setup(c => c.ReceiveAsync(It.IsAny<CancellationToken>()))
            .Returns(new TaskCompletionSource<WebSocketMessage>().Task); // Never completes

        await _sut.ConnectAsync(uri, CancellationToken.None);

        _mockClientAdapter.Verify(c => c.ConnectAsync(uri, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "WSA-006: SetDefaultHeader should delegate to client adapter")]
    public void WSA006()
    {
        _sut.SetDefaultHeader("Authorization", "Bearer token");

        _mockClientAdapter.Verify(c => c.SetDefaultHeader("Authorization", "Bearer token"), Times.Once);
    }

    [Fact(DisplayName = "WSA-007: Dispose should not throw")]
    public void WSA007()
    {
        var adapter = new WebSocketAdapter(_mockLogger.Object, _mockClientAdapter.Object);

        var act = () => adapter.Dispose();

        act.Should().NotThrow();
    }
}
