using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Ave.Extensions.SocketIO.Client.Infrastructure;
using Ave.Extensions.SocketIO.Client.Observers;
using Ave.Extensions.SocketIO.Messages;

namespace Ave.Extensions.SocketIO.Client.Session.EngineIOAdapter;

/// <summary>
/// Base Engine.IO v3 adapter implementation.
/// </summary>
public abstract class EngineIO3Adapter : IEngineIOAdapter, IDisposable
{
    private readonly IStopwatch _stopwatch;
    private readonly ILogger<EngineIO3Adapter> _logger;
    private readonly IDelay _delay;
    private readonly CancellationTokenSource _pingCancellationTokenSource = new CancellationTokenSource();
    private readonly List<IMyObserver<IMessage>> _observers = new List<IMyObserver<IMessage>>();

    private static readonly HashSet<string?> DefaultNamespaces = new HashSet<string?>
    {
        null,
        string.Empty,
        "/"
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="EngineIO3Adapter"/> class.
    /// </summary>
    protected EngineIO3Adapter(IStopwatch stopwatch, ILogger<EngineIO3Adapter> logger, IDelay delay)
    {
        _stopwatch = stopwatch;
        _logger = logger;
        _delay = delay;
    }

    private OpenedMessage? OpenedMessage { get; set; }
    private CancellationTokenSource? _pongTimeoutCts;
    private bool _pongReceived;

    /// <inheritdoc />
    public EngineIOAdapterOptions Options { get; set; } = null!;

    /// <inheritdoc />
    public Action? OnDisconnected { get; set; }

    /// <summary>
    /// Sends a connect message.
    /// </summary>
    protected abstract Task SendConnectAsync();

    /// <summary>
    /// Sends a ping message.
    /// </summary>
    protected abstract Task SendPingAsync();

    /// <summary>
    /// Called when the ping task starts.
    /// </summary>
    protected virtual Task OnPingTaskStarted()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when an opened message is received.
    /// </summary>
    protected virtual void OnOpenedMessageReceived(OpenedMessage message)
    {
    }

    /// <inheritdoc />
    public async Task<bool> ProcessMessageAsync(IMessage message)
    {
        bool shouldSwallow = false;
        switch (message.Type)
        {
            case MessageType.Opened:
                await HandleOpenedMessageAsync(message).ConfigureAwait(false);
                break;
            case MessageType.Connected:
                shouldSwallow = HandleConnectedMessage(message);
                break;
            case MessageType.Pong:
                HandlePongMessage(message);
                break;
            case MessageType.Close:
                OnDisconnected?.Invoke();
                return true;
            case MessageType.Noop:
                return true;
        }

        return shouldSwallow;
    }

    private async Task HandleOpenedMessageAsync(IMessage message)
    {
        OpenedMessage = (OpenedMessage)message;
        OnOpenedMessageReceived(OpenedMessage);
        if (string.IsNullOrEmpty(Options.Namespace))
        {
            return;
        }
        await SendConnectAsync().ConfigureAwait(false);
    }

    private bool HandleConnectedMessage(IMessage message)
    {
        var connectedMessage = (ConnectedMessage)message;
        var shouldSwallow = !DefaultNamespaces.Contains(Options.Namespace)
                            && !Options.Namespace!.Equals(connectedMessage.Namespace, StringComparison.InvariantCultureIgnoreCase);
        if (!shouldSwallow)
        {
            connectedMessage.Sid = OpenedMessage!.Sid;
            Task.Run(StartPingAsync).ConfigureAwait(false);
        }

        return shouldSwallow;
    }

    private async Task StartPingAsync()
    {
        await OnPingTaskStarted().ConfigureAwait(false);
        var token = _pingCancellationTokenSource.Token;
        while (!token.IsCancellationRequested)
        {
            await _delay.DelayAsync(OpenedMessage!.PingInterval, token).ConfigureAwait(false);
            _logger.LogDebug("Sending Ping...");
            _pongReceived = false;
            await SendPingAsync().ConfigureAwait(false);
            _logger.LogDebug("Sent Ping");
            _stopwatch.Restart();
            _ = NotifyObserversAsync(new PingMessage());

            // Wait for pong within pingTimeout
            _pongTimeoutCts?.Cancel();
            _pongTimeoutCts = CancellationTokenSource.CreateLinkedTokenSource(token);
            try
            {
                await _delay.DelayAsync(OpenedMessage.PingTimeout, _pongTimeoutCts.Token).ConfigureAwait(false);
                // Timeout elapsed — check if pong was received
                if (!_pongReceived)
                {
                    _logger.LogDebug("Pong timeout, disconnecting");
                    OnDisconnected?.Invoke();
                    break;
                }
            }
            catch (TaskCanceledException)
            {
                if (token.IsCancellationRequested)
                {
                    break;
                }
                // Pong was received before timeout — continue
            }
        }
    }

    private void HandlePongMessage(IMessage message)
    {
        _pongReceived = true;
        _pongTimeoutCts?.Cancel();
        _stopwatch.Stop();
        var pongMessage = (PongMessage)message;
        pongMessage.Duration = _stopwatch.Elapsed;
    }

    private async Task NotifyObserversAsync(IMessage message)
    {
        foreach (var observer in _observers)
        {
            await observer.OnNextAsync(message).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public void Subscribe(IMyObserver<IMessage> observer)
    {
        if (_observers.Contains(observer))
        {
            return;
        }
        _observers.Add(observer);
    }

    /// <inheritdoc />
    public void Unsubscribe(IMyObserver<IMessage> observer)
    {
        _observers.Remove(observer);
    }

    /// <inheritdoc />
    public void SetOpenedMessage(OpenedMessage message)
    {
        OpenedMessage = message;
    }

    /// <summary>
    /// Disposes resources.
    /// </summary>
    public void Dispose()
    {
        _pingCancellationTokenSource.Cancel();
        _pingCancellationTokenSource.Dispose();
        _pongTimeoutCts?.Cancel();
        _pongTimeoutCts?.Dispose();
    }
}
