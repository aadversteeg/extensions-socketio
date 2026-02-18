using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Ave.Extensions.SocketIO.Client.Infrastructure;
using Ave.Extensions.SocketIO.Client.Protocol.Http;
using Ave.Extensions.SocketIO.Client.Session.EngineIOAdapter;
using Ave.Extensions.SocketIO.Messages;

namespace Ave.Extensions.SocketIO.Client.Session.Http.EngineIOAdapter;

/// <summary>
/// Handles HTTP long-polling for Engine.IO sessions.
/// </summary>
public class PollingHandler : IPollingHandler, IDisposable
{
    private readonly IHttpAdapter _httpAdapter;
    private readonly IRetriable _retryPolicy;
    private readonly ILogger<PollingHandler> _logger;
    private readonly IDelay _delay;
    private OpenedMessage? _openedMessage;
    private readonly CancellationTokenSource _pollingCancellationTokenSource = new CancellationTokenSource();

    /// <summary>
    /// Initializes a new instance of the <see cref="PollingHandler"/> class.
    /// </summary>
    public PollingHandler(
        IHttpAdapter httpAdapter,
        IRetriable retryPolicy,
        ILogger<PollingHandler> logger,
        IDelay delay)
    {
        _httpAdapter = httpAdapter;
        _retryPolicy = retryPolicy;
        _logger = logger;
        _delay = delay;
    }

    /// <inheritdoc />
    public void StartPolling(OpenedMessage message, bool autoUpgrade)
    {
        if (autoUpgrade && message.Upgrades.Contains("websocket"))
        {
            return;
        }
        _openedMessage = message;
        _ = PollingAsync().ConfigureAwait(false);
    }

    private async Task PollingAsync()
    {
        _logger.LogDebug("[PollingAsync] Waiting for HttpAdapter ready...");
        await WaitHttpAdapterReady().ConfigureAwait(false);
        _logger.LogDebug("[PollingAsync] HttpAdapter is ready");
        var token = _pollingCancellationTokenSource.Token;
        while (!token.IsCancellationRequested)
        {
            var request = new HttpRequest();
            _logger.LogDebug("Send Polling request...");
            await _retryPolicy.RetryAsync(2, async () =>
            {
                await _httpAdapter.SendAsync(request, token).ConfigureAwait(false);
            }).ConfigureAwait(false);
            _logger.LogDebug("Sent Polling request");
        }
    }

    /// <inheritdoc />
    public async Task WaitHttpAdapterReady()
    {
        var ms = 0;
        const int interval = 20;
        while (ms < _openedMessage!.PingInterval)
        {
            if (_httpAdapter.IsReadyToSend)
            {
                return;
            }
            await _delay.DelayAsync(interval, CancellationToken.None).ConfigureAwait(false);
            ms += interval;
        }
        var ex = new TimeoutException();
        _logger.LogError(ex, "Wait HttpAdapter ready timeout");
        throw ex;
    }

    /// <summary>
    /// Disposes resources.
    /// </summary>
    public void Dispose()
    {
        _pollingCancellationTokenSource.Cancel();
        _pollingCancellationTokenSource.Dispose();
    }
}
