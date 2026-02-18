using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ave.Extensions.SocketIO.Client.Infrastructure;
using Ave.Extensions.SocketIO.Client.Observers;
using Ave.Extensions.SocketIO.Messages;
using Ave.Extensions.SocketIO.Serialization;

namespace Ave.Extensions.SocketIO.Client.Session.EngineIOAdapter;

/// <summary>
/// Base Engine.IO v4 adapter implementation.
/// </summary>
public abstract class EngineIO4Adapter : IEngineIOAdapter, IDisposable
{
    private readonly IStopwatch _stopwatch;
    private readonly ISerializer _serializer;
    private readonly CancellationTokenSource _pingCancellationTokenSource = new CancellationTokenSource();
    private readonly List<IMyObserver<IMessage>> _observers = new List<IMyObserver<IMessage>>();

    /// <summary>
    /// Initializes a new instance of the <see cref="EngineIO4Adapter"/> class.
    /// </summary>
    protected EngineIO4Adapter(IStopwatch stopwatch, ISerializer serializer)
    {
        _stopwatch = stopwatch;
        _serializer = serializer;
    }

    private OpenedMessage? OpenedMessage { get; set; }

    /// <inheritdoc />
    public EngineIOAdapterOptions Options { get; set; } = null!;

    /// <summary>
    /// Sends a connect message.
    /// </summary>
    protected abstract Task SendConnectAsync(string message);

    /// <summary>
    /// Sends a pong message.
    /// </summary>
    protected abstract Task SendPongAsync();

    /// <summary>
    /// Called when an opened message is received.
    /// </summary>
    protected virtual void OnOpenedMessageReceived(OpenedMessage message)
    {
    }

    /// <inheritdoc />
    public async Task<bool> ProcessMessageAsync(IMessage message)
    {
        switch (message.Type)
        {
            case MessageType.Ping:
                await HandlePingMessageAsync().ConfigureAwait(false);
                break;
            case MessageType.Opened:
                await HandleOpenedMessageAsync(message).ConfigureAwait(false);
                break;
        }

        return false;
    }

    private async Task HandleOpenedMessageAsync(IMessage message)
    {
        OpenedMessage = (OpenedMessage)message;
        OnOpenedMessageReceived(OpenedMessage);

        var builder = new StringBuilder("40");
        if (!string.IsNullOrEmpty(Options.Namespace))
        {
            builder.Append(Options.Namespace).Append(',');
        }
        if (Options.Auth is not null)
        {
            builder.Append(_serializer.Serialize(Options.Auth));
        }
        await SendConnectAsync(builder.ToString()).ConfigureAwait(false);
    }

    private async Task HandlePingMessageAsync()
    {
        _stopwatch.Restart();
        await SendPongAsync().ConfigureAwait(false);
        _stopwatch.Stop();
        var pong = new PongMessage
        {
            Duration = _stopwatch.Elapsed,
        };
        await NotifyObserversAsync(pong).ConfigureAwait(false);
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

    /// <summary>
    /// Disposes resources.
    /// </summary>
    public void Dispose()
    {
        _pingCancellationTokenSource.Cancel();
        _pingCancellationTokenSource.Dispose();
    }
}
