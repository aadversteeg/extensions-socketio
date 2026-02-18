using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Ave.Extensions.SocketIO.Client.Infrastructure;
using Ave.Extensions.SocketIO.Client.Protocol.WebSocket;
using Ave.Extensions.SocketIO.Client.Session.EngineIOAdapter;
using Ave.Extensions.SocketIO.Client.Session.WebSocket.EngineIOAdapter;
using Ave.Extensions.SocketIO.Messages;
using Ave.Extensions.SocketIO.Protocol;
using Ave.Extensions.SocketIO.Serialization;
using Microsoft.Extensions.Logging;

namespace UnitTests.Extensions.SocketIO.Client.Verification;

public class SocketIOConnectVerificationTests
{
    private WebSocketEngineIO4Adapter CreateV4Adapter(
        Mock<IWebSocketAdapter> mockWsAdapter,
        Mock<ISerializer>? mockSerializer = null,
        Mock<IDelay>? mockDelay = null)
    {
        var mockStopwatch = new Mock<IStopwatch>();
        mockSerializer ??= new Mock<ISerializer>();
        mockDelay ??= new Mock<IDelay>();

        // Delay never completes to prevent ping timeout interference
        mockDelay.Setup(d => d.DelayAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns<int, CancellationToken>((_, ct) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                ct.Register(() => tcs.TrySetCanceled());
                return tcs.Task;
            });

        return new WebSocketEngineIO4Adapter(
            mockStopwatch.Object,
            mockSerializer.Object,
            mockDelay.Object,
            mockWsAdapter.Object);
    }

    [Fact(DisplayName = "VSC-001: V4 CONNECT with no namespace/auth should produce '40'")]
    public async Task VSC001()
    {
        var mockWsAdapter = new Mock<IWebSocketAdapter>();
        string? sentMessage = null;

        mockWsAdapter.Setup(w => w.SendAsync(It.IsAny<ProtocolMessage>(), It.IsAny<CancellationToken>()))
            .Callback<ProtocolMessage, CancellationToken>((m, _) => sentMessage = m.Text)
            .Returns(Task.CompletedTask);

        var sut = CreateV4Adapter(mockWsAdapter);
        sut.Options = new EngineIOAdapterOptions
        {
            Timeout = TimeSpan.FromSeconds(5),
        };

        var opened = new OpenedMessage
        {
            Sid = "test-sid",
            PingInterval = 25000,
            PingTimeout = 20000,
            Upgrades = new System.Collections.Generic.List<string>(),
        };

        await sut.ProcessMessageAsync(opened);

        sentMessage.Should().Be("40");

        sut.Dispose();
    }

    [Fact(DisplayName = "VSC-002: V4 CONNECT with namespace should produce '40/admin,'")]
    public async Task VSC002()
    {
        var mockWsAdapter = new Mock<IWebSocketAdapter>();
        string? sentMessage = null;

        mockWsAdapter.Setup(w => w.SendAsync(It.IsAny<ProtocolMessage>(), It.IsAny<CancellationToken>()))
            .Callback<ProtocolMessage, CancellationToken>((m, _) => sentMessage = m.Text)
            .Returns(Task.CompletedTask);

        var sut = CreateV4Adapter(mockWsAdapter);
        sut.Options = new EngineIOAdapterOptions
        {
            Timeout = TimeSpan.FromSeconds(5),
            Namespace = "/admin",
        };

        var opened = new OpenedMessage
        {
            Sid = "test-sid",
            PingInterval = 25000,
            PingTimeout = 20000,
            Upgrades = new System.Collections.Generic.List<string>(),
        };

        await sut.ProcessMessageAsync(opened);

        sentMessage.Should().Be("40/admin,");

        sut.Dispose();
    }

    [Fact(DisplayName = "VSC-003: V4 CONNECT with namespace and auth should produce '40/admin,{\"token\":\"123\"}'")]
    public async Task VSC003()
    {
        var mockWsAdapter = new Mock<IWebSocketAdapter>();
        var mockSerializer = new Mock<ISerializer>();
        string? sentMessage = null;

        mockWsAdapter.Setup(w => w.SendAsync(It.IsAny<ProtocolMessage>(), It.IsAny<CancellationToken>()))
            .Callback<ProtocolMessage, CancellationToken>((m, _) => sentMessage = m.Text)
            .Returns(Task.CompletedTask);

        var auth = new { token = "123" };
        mockSerializer.Setup(s => s.Serialize((object)auth)).Returns("{\"token\":\"123\"}");

        var sut = CreateV4Adapter(mockWsAdapter, mockSerializer);
        sut.Options = new EngineIOAdapterOptions
        {
            Timeout = TimeSpan.FromSeconds(5),
            Namespace = "/admin",
            Auth = auth,
        };

        var opened = new OpenedMessage
        {
            Sid = "test-sid",
            PingInterval = 25000,
            PingTimeout = 20000,
            Upgrades = new System.Collections.Generic.List<string>(),
        };

        await sut.ProcessMessageAsync(opened);

        sentMessage.Should().Be("40/admin,{\"token\":\"123\"}");

        sut.Dispose();
    }

    [Fact(DisplayName = "VSC-004: V4 CONNECT with auth only should produce '40{\"token\":\"123\"}'")]
    public async Task VSC004()
    {
        var mockWsAdapter = new Mock<IWebSocketAdapter>();
        var mockSerializer = new Mock<ISerializer>();
        string? sentMessage = null;

        mockWsAdapter.Setup(w => w.SendAsync(It.IsAny<ProtocolMessage>(), It.IsAny<CancellationToken>()))
            .Callback<ProtocolMessage, CancellationToken>((m, _) => sentMessage = m.Text)
            .Returns(Task.CompletedTask);

        var auth = new { token = "123" };
        mockSerializer.Setup(s => s.Serialize((object)auth)).Returns("{\"token\":\"123\"}");

        var sut = CreateV4Adapter(mockWsAdapter, mockSerializer);
        sut.Options = new EngineIOAdapterOptions
        {
            Timeout = TimeSpan.FromSeconds(5),
            Auth = auth,
        };

        var opened = new OpenedMessage
        {
            Sid = "test-sid",
            PingInterval = 25000,
            PingTimeout = 20000,
            Upgrades = new System.Collections.Generic.List<string>(),
        };

        await sut.ProcessMessageAsync(opened);

        sentMessage.Should().Be("40{\"token\":\"123\"}");

        sut.Dispose();
    }

    [Fact(DisplayName = "VSC-005: V3 CONNECT for default namespace should not send connect")]
    public async Task VSC005()
    {
        var mockStopwatch = new Mock<IStopwatch>();
        var mockLogger = new Mock<ILogger<WebSocketEngineIO3Adapter>>();
        var mockWsAdapter = new Mock<IWebSocketAdapter>();
        var mockDelay = new Mock<IDelay>();

        mockDelay.Setup(d => d.DelayAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns<int, CancellationToken>((_, ct) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                ct.Register(() => tcs.TrySetCanceled());
                return tcs.Task;
            });

        var sut = new WebSocketEngineIO3Adapter(
            mockStopwatch.Object,
            mockLogger.Object,
            mockWsAdapter.Object,
            mockDelay.Object);

        sut.Options = new EngineIOAdapterOptions
        {
            Timeout = TimeSpan.FromSeconds(5),
            // No namespace = default
        };

        var opened = new OpenedMessage
        {
            Sid = "test-sid",
            PingInterval = 25000,
            PingTimeout = 20000,
            Upgrades = new System.Collections.Generic.List<string>(),
        };

        await sut.ProcessMessageAsync(opened);

        // V3 should NOT send connect for default namespace
        mockWsAdapter.Verify(w => w.SendAsync(
            It.IsAny<ProtocolMessage>(),
            It.IsAny<CancellationToken>()), Times.Never);

        sut.Dispose();
    }

    [Fact(DisplayName = "VSC-006: V3 CONNECT for custom namespace should send '40/admin,'")]
    public async Task VSC006()
    {
        var mockStopwatch = new Mock<IStopwatch>();
        var mockLogger = new Mock<ILogger<WebSocketEngineIO3Adapter>>();
        var mockWsAdapter = new Mock<IWebSocketAdapter>();
        var mockDelay = new Mock<IDelay>();
        string? sentMessage = null;

        mockDelay.Setup(d => d.DelayAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns<int, CancellationToken>((_, ct) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                ct.Register(() => tcs.TrySetCanceled());
                return tcs.Task;
            });

        mockWsAdapter.Setup(w => w.SendAsync(It.IsAny<ProtocolMessage>(), It.IsAny<CancellationToken>()))
            .Callback<ProtocolMessage, CancellationToken>((m, _) => sentMessage = m.Text)
            .Returns(Task.CompletedTask);

        var sut = new WebSocketEngineIO3Adapter(
            mockStopwatch.Object,
            mockLogger.Object,
            mockWsAdapter.Object,
            mockDelay.Object);

        sut.Options = new EngineIOAdapterOptions
        {
            Timeout = TimeSpan.FromSeconds(5),
            Namespace = "/admin",
        };

        var opened = new OpenedMessage
        {
            Sid = "test-sid",
            PingInterval = 25000,
            PingTimeout = 20000,
            Upgrades = new System.Collections.Generic.List<string>(),
        };

        await sut.ProcessMessageAsync(opened);

        sentMessage.Should().Be("40/admin,");

        sut.Dispose();
    }

    [Fact(DisplayName = "VSC-007: V4 adapter always sends CONNECT even for default namespace")]
    public async Task VSC007()
    {
        var mockWsAdapter = new Mock<IWebSocketAdapter>();
        string? sentMessage = null;

        mockWsAdapter.Setup(w => w.SendAsync(It.IsAny<ProtocolMessage>(), It.IsAny<CancellationToken>()))
            .Callback<ProtocolMessage, CancellationToken>((m, _) => sentMessage = m.Text)
            .Returns(Task.CompletedTask);

        var sut = CreateV4Adapter(mockWsAdapter);
        sut.Options = new EngineIOAdapterOptions
        {
            Timeout = TimeSpan.FromSeconds(5),
            // No namespace — default
        };

        var opened = new OpenedMessage
        {
            Sid = "test-sid",
            PingInterval = 25000,
            PingTimeout = 20000,
            Upgrades = new System.Collections.Generic.List<string>(),
        };

        await sut.ProcessMessageAsync(opened);

        // Socket.IO v5 requires explicit CONNECT even for default namespace
        mockWsAdapter.Verify(w => w.SendAsync(
            It.Is<ProtocolMessage>(m => m.Text == "40"),
            It.IsAny<CancellationToken>()), Times.Once,
            "V4 adapter must always send CONNECT '40' — no implicit connection in Socket.IO v5");

        sut.Dispose();
    }

    [Fact(DisplayName = "VSC-008: V3 adapter starts ping loop only after ConnectedMessage")]
    public async Task VSC008()
    {
        var mockStopwatch = new Mock<IStopwatch>();
        var mockLogger = new Mock<ILogger<WebSocketEngineIO3Adapter>>();
        var mockWsAdapter = new Mock<IWebSocketAdapter>();
        var mockDelay = new Mock<IDelay>();

        mockDelay.Setup(d => d.DelayAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns<int, CancellationToken>((_, ct) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                ct.Register(() => tcs.TrySetCanceled());
                return tcs.Task;
            });

        mockWsAdapter.Setup(w => w.SendAsync(It.IsAny<ProtocolMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new WebSocketEngineIO3Adapter(
            mockStopwatch.Object,
            mockLogger.Object,
            mockWsAdapter.Object,
            mockDelay.Object);

        sut.Options = new EngineIOAdapterOptions
        {
            Timeout = TimeSpan.FromSeconds(5),
        };

        var opened = new OpenedMessage
        {
            Sid = "test-sid",
            PingInterval = 25000,
            PingTimeout = 5000,
            Upgrades = new System.Collections.Generic.List<string>(),
        };

        // Process opened — should NOT start ping loop yet
        await sut.ProcessMessageAsync(opened);
        await Task.Delay(50);

        // No ping should have been sent after just OpenedMessage
        mockWsAdapter.Verify(w => w.SendAsync(
            It.Is<ProtocolMessage>(m => m.Text == "2"),
            It.IsAny<CancellationToken>()), Times.Never,
            "ping loop should not start until ConnectedMessage is received");

        // No delay calls for ping scheduling either
        mockDelay.Verify(d => d.DelayAsync(25000, It.IsAny<CancellationToken>()), Times.Never,
            "pingInterval delay should not start until ConnectedMessage is received");

        sut.Dispose();
    }
}
