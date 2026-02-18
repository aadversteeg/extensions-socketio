using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Ave.Extensions.SocketIO.Client.Infrastructure;
using Ave.Extensions.SocketIO.Client.Observers;
using Ave.Extensions.SocketIO.Client.Protocol.WebSocket;
using Ave.Extensions.SocketIO.Client.Session;
using Ave.Extensions.SocketIO.Client.Session.EngineIOAdapter;
using Ave.Extensions.SocketIO.Client.Session.WebSocket;
using Ave.Extensions.SocketIO.Client.Session.WebSocket.EngineIOAdapter;
using Ave.Extensions.SocketIO.Messages;
using Ave.Extensions.SocketIO.Protocol;
using Ave.Extensions.SocketIO.Serialization;
using Ave.Extensions.SocketIO;

namespace UnitTests.Extensions.SocketIO.Client.Verification;

public class EngineIOCloseVerificationTests
{
    [Fact(DisplayName = "VEC-001: WebSocket DisconnectAsync sends Socket.IO disconnect '41'")]
    public async Task VEC001()
    {
        var mockLogger = new Mock<ILogger<WebSocketSession>>();
        var mockFactory = new Mock<IEngineIOAdapterFactory>();
        var mockWsAdapter = new Mock<IWebSocketAdapter>();
        var mockSerializer = new Mock<ISerializer>();
        var mockMsgAdapterFactory = new Mock<IEngineIOMessageAdapterFactory>();
        var mockEngineIOAdapter = new Mock<IWebSocketEngineIOAdapter>();

        mockFactory.Setup(f => f.Create<IWebSocketEngineIOAdapter>(It.IsAny<EngineIOCompatibility>()))
            .Returns(mockEngineIOAdapter.Object);
        mockMsgAdapterFactory.Setup(f => f.Create(It.IsAny<EngineIOVersion>()))
            .Returns(new Mock<IEngineIOMessageAdapter>().Object);

        var sut = new WebSocketSession(
            mockLogger.Object,
            mockFactory.Object,
            mockWsAdapter.Object,
            mockSerializer.Object,
            mockMsgAdapterFactory.Object);

        sut.Options = new SessionOptions
        {
            ServerUri = new Uri("http://localhost"),
            EngineIO = EngineIOVersion.V4,
            Timeout = TimeSpan.FromSeconds(5),
        };

        mockWsAdapter.Setup(w => w.SendAsync(It.IsAny<ProtocolMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await sut.DisconnectAsync(CancellationToken.None);

        mockWsAdapter.Verify(w => w.SendAsync(
            It.Is<ProtocolMessage>(m => m.Text == "41"),
            It.IsAny<CancellationToken>()), Times.Once,
            "DisconnectAsync should send Socket.IO disconnect '41'");
    }

    [Fact(DisplayName = "VEC-002: Close packet returns true from V4 ProcessMessageAsync")]
    public async Task VEC002()
    {
        var mockStopwatch = new Mock<IStopwatch>();
        var mockSerializer = new Mock<ISerializer>();
        var mockDelay = new Mock<IDelay>();
        var mockWsAdapter = new Mock<IWebSocketAdapter>();

        var sut = new WebSocketEngineIO4Adapter(
            mockStopwatch.Object,
            mockSerializer.Object,
            mockDelay.Object,
            mockWsAdapter.Object);

        sut.Options = new EngineIOAdapterOptions
        {
            Timeout = TimeSpan.FromSeconds(5),
        };

        var closeMessage = new Mock<IMessage>();
        closeMessage.Setup(m => m.Type).Returns(MessageType.Close);

        var result = await sut.ProcessMessageAsync(closeMessage.Object);

        result.Should().BeTrue("close packet should return true (swallowed)");

        sut.Dispose();
    }

    [Fact(DisplayName = "VEC-003: Close packet fires OnDisconnected callback")]
    public async Task VEC003()
    {
        var mockStopwatch = new Mock<IStopwatch>();
        var mockSerializer = new Mock<ISerializer>();
        var mockDelay = new Mock<IDelay>();
        var mockWsAdapter = new Mock<IWebSocketAdapter>();
        var disconnectedFired = false;

        var sut = new WebSocketEngineIO4Adapter(
            mockStopwatch.Object,
            mockSerializer.Object,
            mockDelay.Object,
            mockWsAdapter.Object);

        sut.Options = new EngineIOAdapterOptions
        {
            Timeout = TimeSpan.FromSeconds(5),
        };

        sut.OnDisconnected = () => disconnectedFired = true;

        var closeMessage = new Mock<IMessage>();
        closeMessage.Setup(m => m.Type).Returns(MessageType.Close);

        await sut.ProcessMessageAsync(closeMessage.Object);

        disconnectedFired.Should().BeTrue("close packet must fire OnDisconnected callback");

        sut.Dispose();
    }

    [Fact(DisplayName = "VEC-004: Close packet does not forward to observers")]
    public async Task VEC004()
    {
        var mockStopwatch = new Mock<IStopwatch>();
        var mockSerializer = new Mock<ISerializer>();
        var mockDelay = new Mock<IDelay>();
        var mockWsAdapter = new Mock<IWebSocketAdapter>();
        var mockObserver = new Mock<IMyObserver<IMessage>>();

        var sut = new WebSocketEngineIO4Adapter(
            mockStopwatch.Object,
            mockSerializer.Object,
            mockDelay.Object,
            mockWsAdapter.Object);

        sut.Options = new EngineIOAdapterOptions
        {
            Timeout = TimeSpan.FromSeconds(5),
        };

        sut.Subscribe(mockObserver.Object);

        var closeMessage = new Mock<IMessage>();
        closeMessage.Setup(m => m.Type).Returns(MessageType.Close);

        await sut.ProcessMessageAsync(closeMessage.Object);

        mockObserver.Verify(o => o.OnNextAsync(It.IsAny<IMessage>()), Times.Never,
            "close packet should not be forwarded to observers");

        sut.Dispose();
    }

    [Fact(DisplayName = "VEC-005: Processing close when OnDisconnected is null does not throw")]
    public async Task VEC005()
    {
        var mockStopwatch = new Mock<IStopwatch>();
        var mockSerializer = new Mock<ISerializer>();
        var mockDelay = new Mock<IDelay>();
        var mockWsAdapter = new Mock<IWebSocketAdapter>();

        var sut = new WebSocketEngineIO4Adapter(
            mockStopwatch.Object,
            mockSerializer.Object,
            mockDelay.Object,
            mockWsAdapter.Object);

        sut.Options = new EngineIOAdapterOptions
        {
            Timeout = TimeSpan.FromSeconds(5),
        };

        // Deliberately do NOT set OnDisconnected
        var closeMessage = new Mock<IMessage>();
        closeMessage.Setup(m => m.Type).Returns(MessageType.Close);

        var act = () => sut.ProcessMessageAsync(closeMessage.Object);

        await act.Should().NotThrowAsync("close packet with null OnDisconnected should not throw");

        sut.Dispose();
    }
}
