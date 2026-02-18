using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Ave.Extensions.SocketIO.Client.Infrastructure;
using Ave.Extensions.SocketIO.Client.Protocol.Http;
using Ave.Extensions.SocketIO.Client.Session.Http.EngineIOAdapter;
using Ave.Extensions.SocketIO.Messages;

namespace UnitTests.Extensions.SocketIO.Client.Session.Http.EngineIOAdapter;

public class PollingHandlerTests
{
    private readonly Mock<IHttpAdapter> _mockHttpAdapter;
    private readonly Mock<IRetriable> _mockRetryPolicy;
    private readonly Mock<ILogger<PollingHandler>> _mockLogger;
    private readonly Mock<IDelay> _mockDelay;
    private readonly PollingHandler _sut;

    public PollingHandlerTests()
    {
        _mockHttpAdapter = new Mock<IHttpAdapter>();
        _mockRetryPolicy = new Mock<IRetriable>();
        _mockLogger = new Mock<ILogger<PollingHandler>>();
        _mockDelay = new Mock<IDelay>();
        _mockDelay.Setup(d => d.DelayAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _sut = new PollingHandler(
            _mockHttpAdapter.Object,
            _mockRetryPolicy.Object,
            _mockLogger.Object,
            _mockDelay.Object);
    }

    [Fact(DisplayName = "PLH-001: StartPolling with autoUpgrade and websocket available should not start polling")]
    public async Task PLH001()
    {
        var message = new OpenedMessage
        {
            Sid = "test",
            PingInterval = 25000,
            PingTimeout = 5000,
            Upgrades = new List<string> { "websocket" },
        };

        _sut.StartPolling(message, autoUpgrade: true);

        // Give time for any async polling to start
        await Task.Delay(100);

        // Should not have called SendAsync on httpAdapter since polling was skipped
        _mockHttpAdapter.Verify(h => h.SendAsync(It.IsAny<HttpRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "PLH-002: WaitHttpAdapterReady when adapter is ready should return immediately")]
    public async Task PLH002()
    {
        _mockHttpAdapter.Setup(h => h.IsReadyToSend).Returns(true);
        // Block the polling loop by making SendAsync never complete
        _mockRetryPolicy.Setup(r => r.RetryAsync(It.IsAny<int>(), It.IsAny<Func<Task>>()))
            .Returns(new TaskCompletionSource<object>().Task);

        var message = new OpenedMessage
        {
            Sid = "test",
            PingInterval = 25000,
            PingTimeout = 5000,
            Upgrades = new List<string>(),
        };
        // Use autoUpgrade=false so _openedMessage is set
        _sut.StartPolling(message, autoUpgrade: false);

        await _sut.WaitHttpAdapterReady();

        // Should not have needed to delay since adapter was ready immediately
        _mockDelay.Verify(d => d.DelayAsync(20, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "PLH-003: WaitHttpAdapterReady when adapter not ready within timeout should throw TimeoutException")]
    public async Task PLH003()
    {
        _mockHttpAdapter.Setup(h => h.IsReadyToSend).Returns(false);
        // Block the polling loop
        _mockRetryPolicy.Setup(r => r.RetryAsync(It.IsAny<int>(), It.IsAny<Func<Task>>()))
            .Returns(new TaskCompletionSource<object>().Task);

        var message = new OpenedMessage
        {
            Sid = "test",
            PingInterval = 100, // Short timeout for fast test
            PingTimeout = 5000,
            Upgrades = new List<string>(),
        };
        _sut.StartPolling(message, autoUpgrade: false);

        var act = () => _sut.WaitHttpAdapterReady();

        await act.Should().ThrowAsync<TimeoutException>();
    }
}
