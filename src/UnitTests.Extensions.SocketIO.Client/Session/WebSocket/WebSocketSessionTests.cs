using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Ave.Extensions.SocketIO;
using Ave.Extensions.SocketIO.Client.Protocol.WebSocket;
using Ave.Extensions.SocketIO.Client.Session;
using Ave.Extensions.SocketIO.Client.Session.EngineIOAdapter;
using Ave.Extensions.SocketIO.Client.Session.WebSocket;
using Ave.Extensions.SocketIO.Client.Session.WebSocket.EngineIOAdapter;
using Ave.Extensions.SocketIO.Messages;
using Ave.Extensions.SocketIO.Protocol;
using Ave.Extensions.SocketIO.Serialization;

namespace UnitTests.Extensions.SocketIO.Client.Session.WebSocket;

public class WebSocketSessionTests
{
    private readonly Mock<ILogger<WebSocketSession>> _mockLogger;
    private readonly Mock<IEngineIOAdapterFactory> _mockFactory;
    private readonly Mock<IWebSocketAdapter> _mockWsAdapter;
    private readonly Mock<ISerializer> _mockSerializer;
    private readonly Mock<IEngineIOMessageAdapterFactory> _mockMsgAdapterFactory;
    private readonly Mock<IWebSocketEngineIOAdapter> _mockEngineIOAdapter;
    private readonly WebSocketSession _sut;

    public WebSocketSessionTests()
    {
        _mockLogger = new Mock<ILogger<WebSocketSession>>();
        _mockFactory = new Mock<IEngineIOAdapterFactory>();
        _mockWsAdapter = new Mock<IWebSocketAdapter>();
        _mockSerializer = new Mock<ISerializer>();
        _mockMsgAdapterFactory = new Mock<IEngineIOMessageAdapterFactory>();
        _mockEngineIOAdapter = new Mock<IWebSocketEngineIOAdapter>();

        _mockFactory.Setup(f => f.Create<IWebSocketEngineIOAdapter>(It.IsAny<EngineIOCompatibility>()))
            .Returns(_mockEngineIOAdapter.Object);
        _mockMsgAdapterFactory.Setup(f => f.Create(It.IsAny<EngineIOVersion>()))
            .Returns(new Mock<IEngineIOMessageAdapter>().Object);

        _sut = new WebSocketSession(
            _mockLogger.Object,
            _mockFactory.Object,
            _mockWsAdapter.Object,
            _mockSerializer.Object,
            _mockMsgAdapterFactory.Object);

        _sut.Options = new SessionOptions
        {
            ServerUri = new Uri("http://localhost"),
            EngineIO = EngineIOVersion.V4,
            Timeout = TimeSpan.FromSeconds(5),
        };
    }

    [Fact(DisplayName = "WSS-001: ConnectAsync should connect via WebSocket adapter")]
    public async Task WSS001()
    {
        _mockWsAdapter.Setup(w => w.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _sut.ConnectAsync(CancellationToken.None);

        _mockWsAdapter.Verify(w => w.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "WSS-002: ConnectAsync with Sid should send upgrade probe message")]
    public async Task WSS002()
    {
        _sut.Options = new SessionOptions
        {
            ServerUri = new Uri("http://localhost"),
            EngineIO = EngineIOVersion.V4,
            Timeout = TimeSpan.FromSeconds(5),
            Sid = "existing-sid",
        };

        _mockWsAdapter.Setup(w => w.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockWsAdapter.Setup(w => w.SendAsync(It.IsAny<ProtocolMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _sut.ConnectAsync(CancellationToken.None);

        _mockWsAdapter.Verify(w => w.SendAsync(
            It.Is<ProtocolMessage>(m => m.Text == "5"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "WSS-003: ConnectAsync without Sid should not send upgrade probe")]
    public async Task WSS003()
    {
        _mockWsAdapter.Setup(w => w.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _sut.ConnectAsync(CancellationToken.None);

        _mockWsAdapter.Verify(w => w.SendAsync(
            It.Is<ProtocolMessage>(m => m.Text == "5"),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "WSS-004: DisconnectAsync with no namespace should send 41")]
    public async Task WSS004()
    {
        _mockWsAdapter.Setup(w => w.SendAsync(It.IsAny<ProtocolMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _sut.DisconnectAsync(CancellationToken.None);

        _mockWsAdapter.Verify(w => w.SendAsync(
            It.Is<ProtocolMessage>(m => m.Text == "41"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "WSS-005: DisconnectAsync with namespace should send 41{namespace},")]
    public async Task WSS005()
    {
        _sut.Options = new SessionOptions
        {
            ServerUri = new Uri("http://localhost"),
            EngineIO = EngineIOVersion.V4,
            Timeout = TimeSpan.FromSeconds(5),
            Namespace = "/test",
        };

        _mockWsAdapter.Setup(w => w.SendAsync(It.IsAny<ProtocolMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _sut.DisconnectAsync(CancellationToken.None);

        _mockWsAdapter.Verify(w => w.SendAsync(
            It.Is<ProtocolMessage>(m => m.Text == "41/test,"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "WSS-006: OnNextAsync with bytes message should call ReadProtocolFrame")]
    public async Task WSS006()
    {
        var inputBytes = new byte[] { 4, 1, 2, 3 };
        var strippedBytes = new byte[] { 1, 2, 3 };
        _mockEngineIOAdapter.Setup(a => a.ReadProtocolFrame(inputBytes))
            .Returns(strippedBytes);

        var message = new ProtocolMessage
        {
            Type = ProtocolMessageType.Bytes,
            Bytes = inputBytes
        };

        // OnNextAsync calls ReadProtocolFrame then HandleMessageAsync -> OnNextBytesMessage
        // OnNextBytesMessage peeks the binary message queue which is empty, throwing InvalidOperationException.
        // This is expected: binary frames should only arrive after a binary text header queues the entry.
        try
        {
            await _sut.OnNextAsync(message);
        }
        catch (System.InvalidOperationException)
        {
            // Expected: Queue.Peek() on empty queue
        }

        _mockEngineIOAdapter.Verify(a => a.ReadProtocolFrame(inputBytes), Times.Once);
    }

    [Fact(DisplayName = "WSS-007: SendAsync should serialize and send via WebSocket adapter")]
    public async Task WSS007()
    {
        var data = new object[] { "event", "data" };
        var protocolMessages = new List<ProtocolMessage>
        {
            new ProtocolMessage { Type = ProtocolMessageType.Text, Text = "42[\"event\",\"data\"]" }
        };
        _mockSerializer.Setup(s => s.Serialize(data))
            .Returns(protocolMessages);
        _mockWsAdapter.Setup(w => w.SendAsync(It.IsAny<ProtocolMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _sut.SendAsync(data, CancellationToken.None);

        _mockSerializer.Verify(s => s.Serialize(data), Times.Once);
        _mockWsAdapter.Verify(w => w.SendAsync(
            It.Is<ProtocolMessage>(m => m.Text == "42[\"event\",\"data\"]"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory(DisplayName = "WSS-008: GetServerUriSchema should map schemes correctly")]
    [InlineData("http://localhost", "ws")]
    [InlineData("https://localhost", "wss")]
    [InlineData("ws://localhost", "ws")]
    [InlineData("wss://localhost", "wss")]
    public async Task WSS008(string serverUri, string expectedScheme)
    {
        _sut.Options = new SessionOptions
        {
            ServerUri = new Uri(serverUri),
            EngineIO = EngineIOVersion.V4,
            Timeout = TimeSpan.FromSeconds(5),
        };

        Uri? capturedUri = null;
        _mockWsAdapter.Setup(w => w.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
            .Callback<Uri, CancellationToken>((u, _) => capturedUri = u)
            .Returns(Task.CompletedTask);

        await _sut.ConnectAsync(CancellationToken.None);

        capturedUri.Should().NotBeNull();
        capturedUri!.Scheme.Should().Be(expectedScheme);
    }
}
