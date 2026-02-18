using System;
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
using Ave.Extensions.SocketIO.Serialization.Decapsulation;

namespace UnitTests.Extensions.SocketIO.Client.Verification;

public class SocketIOEventVerificationTests
{
    [Fact(DisplayName = "VSE-001: Decapsulator should parse EVENT '42[\"event\",\"data\"]'")]
    public void VSE001()
    {
        var decapsulator = new Decapsulator();
        var result = decapsulator.DecapsulateRawText("42[\"event\",\"data\"]");

        result.Success.Should().BeTrue();
        result.Type.Should().Be(MessageType.Event);
        result.Data.Should().Be("[\"event\",\"data\"]");
    }

    [Fact(DisplayName = "VSE-002: Decapsulator should parse EVENT with namespace '42/admin,[\"event\"]'")]
    public void VSE002()
    {
        var decapsulator = new Decapsulator();
        var result = decapsulator.DecapsulateRawText("42/admin,[\"event\"]");

        result.Success.Should().BeTrue();
        result.Type.Should().Be(MessageType.Event);
        result.Data.Should().Be("/admin,[\"event\"]");
    }

    [Fact(DisplayName = "VSE-003: DecapsulateEventMessage should extract ack id")]
    public void VSE003()
    {
        var decapsulator = new Decapsulator();
        var result = decapsulator.DecapsulateEventMessage("12[\"event\"]");

        result.Id.Should().Be(12);
        result.Data.Should().Be("[\"event\"]");
    }

    [Fact(DisplayName = "VSE-004: DecapsulateEventMessage should extract namespace and ack id")]
    public void VSE004()
    {
        var decapsulator = new Decapsulator();
        var result = decapsulator.DecapsulateEventMessage("/admin,12[\"event\"]");

        result.Namespace.Should().Be("/admin");
        result.Id.Should().Be(12);
        result.Data.Should().Be("[\"event\"]");
    }

    [Fact(DisplayName = "VSE-005: DecapsulateBinaryEventMessage should extract bytes count and data")]
    public void VSE005()
    {
        var decapsulator = new Decapsulator();
        var result = decapsulator.DecapsulateBinaryEventMessage("1-[\"event\",{\"_placeholder\":true,\"num\":0}]");

        result.BytesCount.Should().Be(1);
        result.Data.Should().Be("[\"event\",{\"_placeholder\":true,\"num\":0}]");
    }

    [Fact(DisplayName = "VSE-006: DecapsulateBinaryEventMessage should extract namespace")]
    public void VSE006()
    {
        var decapsulator = new Decapsulator();
        var result = decapsulator.DecapsulateBinaryEventMessage("1-/admin,12[\"event\",{\"_placeholder\":true,\"num\":0}]");

        result.BytesCount.Should().Be(1);
        result.Namespace.Should().Be("/admin");
        result.Id.Should().Be(12);
        result.Data.Should().Be("[\"event\",{\"_placeholder\":true,\"num\":0}]");
    }

    [Fact(DisplayName = "VSE-007: DISCONNECT encoding should be '41' for default namespace")]
    public void VSE007()
    {
        var decapsulator = new Decapsulator();
        var result = decapsulator.DecapsulateRawText("41");

        result.Success.Should().BeTrue();
        result.Type.Should().Be(MessageType.Disconnected);
        result.Data.Should().BeEmpty();
    }

    [Fact(DisplayName = "VSE-008: DISCONNECT with namespace should be '41/admin,'")]
    public void VSE008()
    {
        var decapsulator = new Decapsulator();
        var result = decapsulator.DecapsulateRawText("41/admin,");

        result.Success.Should().BeTrue();
        result.Type.Should().Be(MessageType.Disconnected);
        result.Data.Should().Be("/admin,");
    }

    [Fact(DisplayName = "VSE-009: Decapsulator should correctly identify Engine.IO close packet")]
    public void VSE009()
    {
        var decapsulator = new Decapsulator();
        var result = decapsulator.DecapsulateRawText("1");

        result.Success.Should().BeTrue();
        result.Type.Should().Be(MessageType.Close);
        result.Data.Should().BeEmpty();
    }

    [Fact(DisplayName = "VSE-010: Decapsulator should correctly identify Engine.IO noop packet")]
    public void VSE010()
    {
        var decapsulator = new Decapsulator();
        var result = decapsulator.DecapsulateRawText("6");

        result.Success.Should().BeTrue();
        result.Type.Should().Be(MessageType.Noop);
        result.Data.Should().BeEmpty();
    }

    [Fact(DisplayName = "VSE-011: Decapsulator should correctly identify Engine.IO upgrade packet")]
    public void VSE011()
    {
        var decapsulator = new Decapsulator();
        var result = decapsulator.DecapsulateRawText("5");

        result.Success.Should().BeTrue();
        result.Type.Should().Be(MessageType.Upgrade);
        result.Data.Should().BeEmpty();
    }

    [Fact(DisplayName = "VSE-012: WebSocket DISCONNECT should send '41' for default namespace")]
    public async Task VSE012()
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
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "VSE-013: WebSocket DISCONNECT should send '41/admin,' for custom namespace")]
    public async Task VSE013()
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
            Namespace = "/admin",
        };

        mockWsAdapter.Setup(w => w.SendAsync(It.IsAny<ProtocolMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await sut.DisconnectAsync(CancellationToken.None);

        mockWsAdapter.Verify(w => w.SendAsync(
            It.Is<ProtocolMessage>(m => m.Text == "41/admin,"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "VSE-014: Decapsulator should parse ping '2' as MessageType.Ping")]
    public void VSE014()
    {
        var decapsulator = new Decapsulator();
        var result = decapsulator.DecapsulateRawText("2");

        result.Success.Should().BeTrue();
        result.Type.Should().Be(MessageType.Ping);
        result.Data.Should().BeEmpty();
    }

    [Fact(DisplayName = "VSE-015: Decapsulator should parse pong '3' as MessageType.Pong")]
    public void VSE015()
    {
        var decapsulator = new Decapsulator();
        var result = decapsulator.DecapsulateRawText("3");

        result.Success.Should().BeTrue();
        result.Type.Should().Be(MessageType.Pong);
        result.Data.Should().BeEmpty();
    }
}
