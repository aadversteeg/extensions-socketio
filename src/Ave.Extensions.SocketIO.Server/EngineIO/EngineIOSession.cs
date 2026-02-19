using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ave.Extensions.SocketIO.Protocol;

namespace Ave.Extensions.SocketIO.Server.EngineIO;

/// <summary>
/// Engine.IO session implementation managing transport-level communication with a single client.
/// </summary>
public class EngineIOSession : IEngineIOSession
{
    private readonly ConcurrentQueue<ProtocolMessage> _sendQueue = new ConcurrentQueue<ProtocolMessage>();
    private TaskCompletionSource<bool>? _pollTcs;
    private readonly object _pollLock = new object();
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="EngineIOSession"/> class.
    /// </summary>
    public EngineIOSession(string sid, EngineIOVersion version, TransportProtocol transport)
    {
        Sid = sid;
        Version = version;
        CurrentTransport = transport;
        IsOpen = true;
    }

    /// <inheritdoc />
    public string Sid { get; }

    /// <inheritdoc />
    public EngineIOVersion Version { get; }

    /// <inheritdoc />
    public TransportProtocol CurrentTransport { get; private set; }

    /// <inheritdoc />
    public bool IsOpen { get; private set; }

    /// <inheritdoc />
    public bool IsUpgrading { get; private set; }

    /// <inheritdoc />
    public Func<ProtocolMessage, Task>? OnMessage { get; set; }

    /// <inheritdoc />
    public Action? OnClose { get; set; }

    /// <summary>
    /// Gets or sets the send callback for WebSocket transport.
    /// When set, messages are sent directly via this callback instead of being queued.
    /// </summary>
    public Func<ProtocolMessage, CancellationToken, Task>? WebSocketSend { get; set; }

    /// <inheritdoc />
    public Task SendAsync(ProtocolMessage message, CancellationToken cancellationToken)
    {
        if (!IsOpen) return Task.CompletedTask;

        if (CurrentTransport == TransportProtocol.WebSocket && WebSocketSend != null)
        {
            return WebSocketSend(message, cancellationToken);
        }

        _sendQueue.Enqueue(message);
        SignalPoll();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SendAsync(string text, CancellationToken cancellationToken)
    {
        return SendAsync(new ProtocolMessage { Type = ProtocolMessageType.Text, Text = text }, cancellationToken);
    }

    /// <inheritdoc />
    public Task SendAsync(byte[] bytes, CancellationToken cancellationToken)
    {
        return SendAsync(new ProtocolMessage { Type = ProtocolMessageType.Bytes, Bytes = bytes }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task ReceiveAsync(ProtocolMessage message)
    {
        if (!IsOpen) return;

        var handler = OnMessage;
        if (handler != null)
        {
            await handler(message).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public Task<ProtocolMessage[]> DrainAsync(CancellationToken cancellationToken)
    {
        // If there are already queued messages, return them immediately
        if (!_sendQueue.IsEmpty)
        {
            return Task.FromResult(DrainQueue());
        }

        // Otherwise wait for messages to arrive
        TaskCompletionSource<bool> tcs;
        lock (_pollLock)
        {
            if (!_sendQueue.IsEmpty)
            {
                return Task.FromResult(DrainQueue());
            }

            tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pollTcs = tcs;
        }

        cancellationToken.Register(() => tcs.TrySetCanceled());

        return tcs.Task.ContinueWith(_ => DrainQueue(), TaskContinuationOptions.ExecuteSynchronously);
    }

    /// <inheritdoc />
    public void UpgradeTransport()
    {
        IsUpgrading = false;
        CurrentTransport = TransportProtocol.WebSocket;
    }

    /// <inheritdoc />
    public Task CloseAsync()
    {
        if (!IsOpen) return Task.CompletedTask;

        IsOpen = false;
        SignalPoll();
        OnClose?.Invoke();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        IsOpen = false;
        SignalPoll();
    }

    private ProtocolMessage[] DrainQueue()
    {
        var messages = _sendQueue.ToArray();
        while (_sendQueue.TryDequeue(out _)) { }
        return messages;
    }

    private void SignalPoll()
    {
        lock (_pollLock)
        {
            _pollTcs?.TrySetResult(true);
            _pollTcs = null;
        }
    }
}
