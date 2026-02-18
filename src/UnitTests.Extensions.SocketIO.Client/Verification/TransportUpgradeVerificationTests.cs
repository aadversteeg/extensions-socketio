using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Ave.Extensions.SocketIO;
using Ave.Extensions.SocketIO.Client.Infrastructure;
using Ave.Extensions.SocketIO.Client.Protocol.WebSocket;
using Ave.Extensions.SocketIO.Client.Session;
using Ave.Extensions.SocketIO.Client.Session.EngineIOAdapter;
using Ave.Extensions.SocketIO.Client.Session.WebSocket;
using Ave.Extensions.SocketIO.Client.Session.WebSocket.EngineIOAdapter;
using Ave.Extensions.SocketIO.Messages;
using Ave.Extensions.SocketIO.Protocol;
using Ave.Extensions.SocketIO.Serialization;

namespace UnitTests.Extensions.SocketIO.Client.Verification;

public class TransportUpgradeVerificationTests
{
    private readonly Mock<ILogger<WebSocketSession>> _mockLogger;
    private readonly Mock<IEngineIOAdapterFactory> _mockFactory;
    private readonly Mock<IWebSocketAdapter> _mockWsAdapter;
    private readonly Mock<ISerializer> _mockSerializer;
    private readonly Mock<IEngineIOMessageAdapterFactory> _mockMsgAdapterFactory;
    private readonly Mock<IWebSocketEngineIOAdapter> _mockEngineIOAdapter;
    private readonly WebSocketSession _sut;

    public TransportUpgradeVerificationTests()
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
    }

    [Fact(DisplayName = "VTU-001: Upgrade should send probe ping '2probe' before upgrade packet '5'")]
    public async Task VTU001()
    {
        _sut.Options = new SessionOptions
        {
            ServerUri = new Uri("http://localhost"),
            EngineIO = EngineIOVersion.V4,
            Timeout = TimeSpan.FromSeconds(5),
            Sid = "existing-sid",
        };

        var sentMessages = new List<string>();

        _mockWsAdapter.Setup(w => w.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockWsAdapter.Setup(w => w.SendAsync(It.Is<ProtocolMessage>(m => m.Text == "2probe"), It.IsAny<CancellationToken>()))
            .Returns<ProtocolMessage, CancellationToken>(async (msg, _) =>
            {
                sentMessages.Add(msg.Text!);
                await Task.Yield();
                // Simulate server responding with "3probe"
                await _sut.OnNextAsync(new ProtocolMessage { Type = ProtocolMessageType.Text, Text = "3probe" });
            });

        _mockWsAdapter.Setup(w => w.SendAsync(It.Is<ProtocolMessage>(m => m.Text == "5"), It.IsAny<CancellationToken>()))
            .Returns<ProtocolMessage, CancellationToken>((msg, _) =>
            {
                sentMessages.Add(msg.Text!);
                return Task.CompletedTask;
            });

        await _sut.ConnectAsync(CancellationToken.None);

        sentMessages.Should().ContainInOrder("2probe", "5");
    }

    [Fact(DisplayName = "VTU-002: Upgrade should wait for '3probe' response")]
    public async Task VTU002()
    {
        _sut.Options = new SessionOptions
        {
            ServerUri = new Uri("http://localhost"),
            EngineIO = EngineIOVersion.V4,
            Timeout = TimeSpan.FromSeconds(1),
            Sid = "existing-sid",
        };

        _mockWsAdapter.Setup(w => w.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Don't simulate "3probe" response — the probe should timeout
        _mockWsAdapter.Setup(w => w.SendAsync(It.IsAny<ProtocolMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var act = () => _sut.ConnectAsync(CancellationToken.None);

        await act.Should().ThrowAsync<ConnectionFailedException>("should timeout waiting for 3probe");
    }

    [Fact(DisplayName = "VTU-003: WebSocket connection should include 'sid' query parameter")]
    public async Task VTU003()
    {
        _sut.Options = new SessionOptions
        {
            ServerUri = new Uri("http://localhost"),
            EngineIO = EngineIOVersion.V4,
            Timeout = TimeSpan.FromSeconds(5),
            Sid = "test-session-id",
        };

        Uri? capturedUri = null;
        _mockWsAdapter.Setup(w => w.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
            .Callback<Uri, CancellationToken>((u, _) => capturedUri = u)
            .Returns(Task.CompletedTask);

        _mockWsAdapter.Setup(w => w.SendAsync(It.Is<ProtocolMessage>(m => m.Text == "2probe"), It.IsAny<CancellationToken>()))
            .Returns<ProtocolMessage, CancellationToken>(async (_, _) =>
            {
                await Task.Yield();
                await _sut.OnNextAsync(new ProtocolMessage { Type = ProtocolMessageType.Text, Text = "3probe" });
            });
        _mockWsAdapter.Setup(w => w.SendAsync(It.Is<ProtocolMessage>(m => m.Text == "5"), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _sut.ConnectAsync(CancellationToken.None);

        capturedUri.Should().NotBeNull();
        capturedUri!.Query.Should().Contain("sid=test-session-id");
    }

    [Fact(DisplayName = "VTU-004: Fresh connection without Sid should not send probe or upgrade")]
    public async Task VTU004()
    {
        _sut.Options = new SessionOptions
        {
            ServerUri = new Uri("http://localhost"),
            EngineIO = EngineIOVersion.V4,
            Timeout = TimeSpan.FromSeconds(5),
        };

        _mockWsAdapter.Setup(w => w.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _sut.ConnectAsync(CancellationToken.None);

        _mockWsAdapter.Verify(w => w.SendAsync(
            It.Is<ProtocolMessage>(m => m.Text == "2probe"),
            It.IsAny<CancellationToken>()), Times.Never);
        _mockWsAdapter.Verify(w => w.SendAsync(
            It.Is<ProtocolMessage>(m => m.Text == "5"),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "VTU-005: Probe message must be exactly '2probe' (case-sensitive)")]
    public async Task VTU005()
    {
        _sut.Options = new SessionOptions
        {
            ServerUri = new Uri("http://localhost"),
            EngineIO = EngineIOVersion.V4,
            Timeout = TimeSpan.FromSeconds(5),
            Sid = "existing-sid",
        };

        string? capturedProbe = null;

        _mockWsAdapter.Setup(w => w.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockWsAdapter.Setup(w => w.SendAsync(It.Is<ProtocolMessage>(m => m.Text != null && m.Text.Contains("probe")), It.IsAny<CancellationToken>()))
            .Returns<ProtocolMessage, CancellationToken>(async (msg, _) =>
            {
                capturedProbe = msg.Text;
                await Task.Yield();
                await _sut.OnNextAsync(new ProtocolMessage { Type = ProtocolMessageType.Text, Text = "3probe" });
            });

        _mockWsAdapter.Setup(w => w.SendAsync(It.Is<ProtocolMessage>(m => m.Text == "5"), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _sut.ConnectAsync(CancellationToken.None);

        capturedProbe.Should().Be("2probe", "probe message must be exactly '2probe' (case-sensitive, matching socket.ts:314)");
    }

    [Fact(DisplayName = "VTU-006: Upgrade packet must be exactly '5'")]
    public async Task VTU006()
    {
        _sut.Options = new SessionOptions
        {
            ServerUri = new Uri("http://localhost"),
            EngineIO = EngineIOVersion.V4,
            Timeout = TimeSpan.FromSeconds(5),
            Sid = "existing-sid",
        };

        string? capturedUpgrade = null;

        _mockWsAdapter.Setup(w => w.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockWsAdapter.Setup(w => w.SendAsync(It.Is<ProtocolMessage>(m => m.Text == "2probe"), It.IsAny<CancellationToken>()))
            .Returns<ProtocolMessage, CancellationToken>(async (_, _) =>
            {
                await Task.Yield();
                await _sut.OnNextAsync(new ProtocolMessage { Type = ProtocolMessageType.Text, Text = "3probe" });
            });

        _mockWsAdapter.Setup(w => w.SendAsync(It.Is<ProtocolMessage>(m => m.Text != null && m.Text != "2probe"), It.IsAny<CancellationToken>()))
            .Callback<ProtocolMessage, CancellationToken>((msg, _) => { if (msg.Text != "2probe") capturedUpgrade = msg.Text; })
            .Returns(Task.CompletedTask);

        await _sut.ConnectAsync(CancellationToken.None);

        capturedUpgrade.Should().Be("5", "upgrade packet must be exactly '5' (matching socket.ts:320)");
    }

    [Fact(DisplayName = "VTU-007: Noop received during upgrade should be swallowed")]
    public async Task VTU007()
    {
        var mockStopwatch = new Mock<IStopwatch>();
        var mockSerializer = new Mock<ISerializer>();
        var mockDelay = new Mock<IDelay>();
        var mockWsAdapter = new Mock<IWebSocketAdapter>();

        mockDelay.Setup(d => d.DelayAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns<int, CancellationToken>((_, ct) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                ct.Register(() => tcs.TrySetCanceled());
                return tcs.Task;
            });

        var adapter = new WebSocketEngineIO4Adapter(
            mockStopwatch.Object,
            mockSerializer.Object,
            mockDelay.Object,
            mockWsAdapter.Object);

        adapter.Options = new EngineIOAdapterOptions
        {
            Timeout = TimeSpan.FromSeconds(5),
        };

        // Noop message (type 6) — sent by server during upgrade to signal buffer flush
        var noopMessage = new Mock<IMessage>();
        noopMessage.Setup(m => m.Type).Returns(MessageType.Noop);

        var result = await adapter.ProcessMessageAsync(noopMessage.Object);

        result.Should().BeTrue("noop packet during upgrade should be swallowed (matching socket.ts:344)");

        adapter.Dispose();
    }
}
