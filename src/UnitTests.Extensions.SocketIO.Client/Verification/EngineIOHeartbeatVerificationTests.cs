using System;
using System.Linq;
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

public class EngineIOHeartbeatVerificationTests
{
    [Fact(DisplayName = "VHB-001: V4 adapter should send pong(3) in response to ping(2)")]
    public async Task VHB001()
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

        var sut = new WebSocketEngineIO4Adapter(
            mockStopwatch.Object,
            mockSerializer.Object,
            mockDelay.Object,
            mockWsAdapter.Object);

        sut.Options = new EngineIOAdapterOptions
        {
            Timeout = TimeSpan.FromSeconds(5),
        };

        mockWsAdapter.Setup(w => w.SendAsync(It.IsAny<ProtocolMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var ping = new PingMessage();
        var result = await sut.ProcessMessageAsync(ping);

        result.Should().BeTrue("ping should be swallowed");
        mockWsAdapter.Verify(w => w.SendAsync(
            It.Is<ProtocolMessage>(m => m.Text == "3"),
            It.IsAny<CancellationToken>()), Times.Once);

        sut.Dispose();
    }

    [Fact(DisplayName = "VHB-002: V4 adapter should disconnect on ping timeout")]
    public async Task VHB002()
    {
        var mockStopwatch = new Mock<IStopwatch>();
        var mockSerializer = new Mock<ISerializer>();
        var mockDelay = new Mock<IDelay>();
        var mockWsAdapter = new Mock<IWebSocketAdapter>();
        var disconnected = false;

        // For the ping timeout monitor: first delay completes immediately (timeout fires)
        mockDelay.Setup(d => d.DelayAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mockWsAdapter.Setup(w => w.SendAsync(It.IsAny<ProtocolMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new WebSocketEngineIO4Adapter(
            mockStopwatch.Object,
            mockSerializer.Object,
            mockDelay.Object,
            mockWsAdapter.Object);

        sut.Options = new EngineIOAdapterOptions
        {
            Timeout = TimeSpan.FromSeconds(5),
        };

        sut.OnDisconnected = () => disconnected = true;

        // Process opened message to start ping timeout monitor
        var opened = new OpenedMessage
        {
            Sid = "test-sid",
            PingInterval = 25000,
            PingTimeout = 20000,
            Upgrades = new System.Collections.Generic.List<string>(),
        };

        await sut.ProcessMessageAsync(opened);

        // Give the background task time to fire
        await Task.Delay(100);

        disconnected.Should().BeTrue("adapter should disconnect when ping timeout fires");

        sut.Dispose();
    }

    [Fact(DisplayName = "VHB-003: V4 adapter should reset timeout on each ping")]
    public async Task VHB003()
    {
        var mockStopwatch = new Mock<IStopwatch>();
        var mockSerializer = new Mock<ISerializer>();
        var mockDelay = new Mock<IDelay>();
        var mockWsAdapter = new Mock<IWebSocketAdapter>();
        var disconnected = false;

        // Delay never completes unless cancelled
        mockDelay.Setup(d => d.DelayAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns<int, CancellationToken>((_, ct) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                ct.Register(() => tcs.TrySetCanceled());
                return tcs.Task;
            });

        mockWsAdapter.Setup(w => w.SendAsync(It.IsAny<ProtocolMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new WebSocketEngineIO4Adapter(
            mockStopwatch.Object,
            mockSerializer.Object,
            mockDelay.Object,
            mockWsAdapter.Object);

        sut.Options = new EngineIOAdapterOptions
        {
            Timeout = TimeSpan.FromSeconds(5),
        };

        sut.OnDisconnected = () => disconnected = true;

        // Start monitoring
        var opened = new OpenedMessage
        {
            Sid = "test-sid",
            PingInterval = 25000,
            PingTimeout = 20000,
            Upgrades = new System.Collections.Generic.List<string>(),
        };

        await sut.ProcessMessageAsync(opened);
        await Task.Delay(50);

        // Process a ping — this should reset the timer
        var ping = new PingMessage();
        await sut.ProcessMessageAsync(ping);

        // Verify delay was called at least twice (initial + reset)
        mockDelay.Verify(d => d.DelayAsync(45000, It.IsAny<CancellationToken>()), Times.AtLeast(2));

        disconnected.Should().BeFalse("timeout should not have fired since we keep receiving pings");

        sut.Dispose();
    }

    [Fact(DisplayName = "VHB-004: V3 adapter should send ping(2) at pingInterval")]
    public async Task VHB004()
    {
        var mockStopwatch = new Mock<IStopwatch>();
        var mockLogger = new Mock<ILogger<WebSocketEngineIO3Adapter>>();
        var mockWsAdapter = new Mock<IWebSocketAdapter>();
        var mockDelay = new Mock<IDelay>();

        var pingIntervalCallCount = 0;
        mockDelay.Setup(d => d.DelayAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns<int, CancellationToken>((ms, ct) =>
            {
                pingIntervalCallCount++;
                if (pingIntervalCallCount <= 1)
                {
                    return Task.CompletedTask; // Let the first delay (pingInterval) complete
                }
                // All subsequent delays hang (pong timeout, next interval)
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

        // Process opened message
        var opened = new OpenedMessage
        {
            Sid = "test-sid",
            PingInterval = 25000,
            PingTimeout = 5000,
            Upgrades = new System.Collections.Generic.List<string>(),
        };

        await sut.ProcessMessageAsync(opened);

        // Process connected message to start ping loop (default namespace triggers it)
        var connected = new ConnectedMessage
        {
            Namespace = "/",
        };

        await sut.ProcessMessageAsync(connected);

        // Give the background task time to run
        await Task.Delay(100);

        // Verify that "2" (ping) was sent
        mockWsAdapter.Verify(w => w.SendAsync(
            It.Is<ProtocolMessage>(m => m.Text == "2"),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);

        sut.Dispose();
    }

    [Fact(DisplayName = "VHB-005: V3 adapter should disconnect on pong timeout")]
    public async Task VHB005()
    {
        var mockStopwatch = new Mock<IStopwatch>();
        var mockLogger = new Mock<ILogger<WebSocketEngineIO3Adapter>>();
        var mockWsAdapter = new Mock<IWebSocketAdapter>();
        var mockDelay = new Mock<IDelay>();
        var disconnected = false;

        // All delays complete immediately (pingInterval + pongTimeout)
        mockDelay.Setup(d => d.DelayAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

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

        sut.OnDisconnected = () => disconnected = true;

        // Process opened
        var opened = new OpenedMessage
        {
            Sid = "test-sid",
            PingInterval = 25000,
            PingTimeout = 5000,
            Upgrades = new System.Collections.Generic.List<string>(),
        };

        await sut.ProcessMessageAsync(opened);

        // Process connected to start ping loop
        var connected = new ConnectedMessage
        {
            Namespace = "/",
        };

        await sut.ProcessMessageAsync(connected);

        // Give the background task time to fire timeout (no pong received)
        await Task.Delay(200);

        disconnected.Should().BeTrue("adapter should disconnect when no pong is received within timeout");

        sut.Dispose();
    }

    [Fact(DisplayName = "VHB-006: V4 ping timeout delay should use pingInterval + pingTimeout ms")]
    public async Task VHB006()
    {
        var mockStopwatch = new Mock<IStopwatch>();
        var mockSerializer = new Mock<ISerializer>();
        var mockDelay = new Mock<IDelay>();
        var mockWsAdapter = new Mock<IWebSocketAdapter>();
        int? capturedDelayMs = null;

        mockDelay.Setup(d => d.DelayAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns<int, CancellationToken>((ms, ct) =>
            {
                capturedDelayMs ??= ms;
                var tcs = new TaskCompletionSource<bool>();
                ct.Register(() => tcs.TrySetCanceled());
                return tcs.Task;
            });

        mockWsAdapter.Setup(w => w.SendAsync(It.IsAny<ProtocolMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new WebSocketEngineIO4Adapter(
            mockStopwatch.Object,
            mockSerializer.Object,
            mockDelay.Object,
            mockWsAdapter.Object);

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
        await Task.Delay(50);

        capturedDelayMs.Should().Be(45000, "ping timeout should be pingInterval(25000) + pingTimeout(20000)");

        sut.Dispose();
    }

    [Fact(DisplayName = "VHB-007: V4 ping should produce PongMessage with Duration via observers")]
    public async Task VHB007()
    {
        var mockStopwatch = new Mock<IStopwatch>();
        var mockSerializer = new Mock<ISerializer>();
        var mockDelay = new Mock<IDelay>();
        var mockWsAdapter = new Mock<IWebSocketAdapter>();
        var mockObserver = new Mock<Ave.Extensions.SocketIO.Client.Observers.IMyObserver<IMessage>>();
        IMessage? observedMessage = null;

        mockStopwatch.Setup(s => s.Elapsed).Returns(TimeSpan.FromMilliseconds(42));

        mockDelay.Setup(d => d.DelayAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns<int, CancellationToken>((_, ct) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                ct.Register(() => tcs.TrySetCanceled());
                return tcs.Task;
            });

        mockWsAdapter.Setup(w => w.SendAsync(It.IsAny<ProtocolMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mockObserver.Setup(o => o.OnNextAsync(It.IsAny<IMessage>()))
            .Callback<IMessage>(m => observedMessage = m)
            .Returns(Task.CompletedTask);

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

        var ping = new PingMessage();
        await sut.ProcessMessageAsync(ping);

        observedMessage.Should().NotBeNull();
        observedMessage.Should().BeOfType<PongMessage>();
        ((PongMessage)observedMessage!).Duration.Should().Be(TimeSpan.FromMilliseconds(42));

        sut.Dispose();
    }

    [Fact(DisplayName = "VHB-008: V3 pong message should record Duration via stopwatch")]
    public async Task VHB008()
    {
        var mockStopwatch = new Mock<IStopwatch>();
        var mockLogger = new Mock<ILogger<WebSocketEngineIO3Adapter>>();
        var mockWsAdapter = new Mock<IWebSocketAdapter>();
        var mockDelay = new Mock<IDelay>();

        mockStopwatch.Setup(s => s.Elapsed).Returns(TimeSpan.FromMilliseconds(55));

        var pingIntervalCallCount = 0;
        mockDelay.Setup(d => d.DelayAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns<int, CancellationToken>((ms, ct) =>
            {
                pingIntervalCallCount++;
                if (pingIntervalCallCount <= 1)
                {
                    return Task.CompletedTask;
                }
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

        await sut.ProcessMessageAsync(opened);

        var connected = new ConnectedMessage { Namespace = "/" };
        await sut.ProcessMessageAsync(connected);

        await Task.Delay(100);

        // Now send a pong to the adapter
        var pong = new PongMessage();
        await sut.ProcessMessageAsync(pong);

        pong.Duration.Should().Be(TimeSpan.FromMilliseconds(55),
            "V3 adapter should record stopwatch elapsed time on pong");

        sut.Dispose();
    }

    [Fact(DisplayName = "VHB-009: V4 adapter should never send ping '2' (only pong '3')")]
    public async Task VHB009()
    {
        var mockStopwatch = new Mock<IStopwatch>();
        var mockSerializer = new Mock<ISerializer>();
        var mockDelay = new Mock<IDelay>();
        var mockWsAdapter = new Mock<IWebSocketAdapter>();
        var sentMessages = new System.Collections.Generic.List<string>();

        mockDelay.Setup(d => d.DelayAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns<int, CancellationToken>((_, ct) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                ct.Register(() => tcs.TrySetCanceled());
                return tcs.Task;
            });

        mockWsAdapter.Setup(w => w.SendAsync(It.IsAny<ProtocolMessage>(), It.IsAny<CancellationToken>()))
            .Callback<ProtocolMessage, CancellationToken>((m, _) => { if (m.Text != null) sentMessages.Add(m.Text); })
            .Returns(Task.CompletedTask);

        var sut = new WebSocketEngineIO4Adapter(
            mockStopwatch.Object,
            mockSerializer.Object,
            mockDelay.Object,
            mockWsAdapter.Object);

        sut.Options = new EngineIOAdapterOptions
        {
            Timeout = TimeSpan.FromSeconds(5),
        };

        // Process opened
        var opened = new OpenedMessage
        {
            Sid = "test-sid",
            PingInterval = 25000,
            PingTimeout = 20000,
            Upgrades = new System.Collections.Generic.List<string>(),
        };

        await sut.ProcessMessageAsync(opened);

        // Process multiple pings
        for (int i = 0; i < 3; i++)
        {
            await sut.ProcessMessageAsync(new PingMessage());
        }

        // V4 adapter should only send pong "3", never ping "2"
        sentMessages.Where(m => m == "2").Should().BeEmpty("V4 adapter must never send ping '2'");
        sentMessages.Where(m => m == "3").Should().HaveCount(3, "V4 adapter should send pong '3' for each ping");

        sut.Dispose();
    }
}
