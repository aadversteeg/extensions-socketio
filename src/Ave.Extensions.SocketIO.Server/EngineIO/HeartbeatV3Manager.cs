using System;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Ave.Extensions.SocketIO.Server.EngineIO;

/// <summary>
/// Engine.IO v3 heartbeat manager. Client sends ping, server responds with pong.
/// Server monitors for client pings and closes session on timeout.
/// </summary>
public class HeartbeatV3Manager : IHeartbeatManager
{
    private readonly int _pingInterval;
    private readonly int _pingTimeout;
    private readonly ILogger<HeartbeatV3Manager> _logger;
    private Timer? _timeoutTimer;
    private IEngineIOSession? _session;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="HeartbeatV3Manager"/> class.
    /// </summary>
    public HeartbeatV3Manager(SocketIOServerOptions options, ILogger<HeartbeatV3Manager> logger)
    {
        _pingInterval = options.PingInterval;
        _pingTimeout = options.PingTimeout;
        _logger = logger;
    }

    /// <inheritdoc />
    public void Start(IEngineIOSession session)
    {
        _session = session;
        ResetTimeout();
    }

    /// <inheritdoc />
    public void HandlePing()
    {
        if (_disposed || _session == null || !_session.IsOpen) return;

        _logger.LogDebug("Received ping from session {Sid}, sending pong", _session.Sid);
        _session.SendAsync("3", CancellationToken.None).ConfigureAwait(false);
        ResetTimeout();
    }

    /// <inheritdoc />
    public void HandlePong()
    {
        // In v3, server doesn't send ping — client pong is a no-op
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timeoutTimer?.Dispose();
    }

    private void ResetTimeout()
    {
        if (_disposed) return;

        _timeoutTimer?.Dispose();
        var timeout = _pingInterval + _pingTimeout;
        _timeoutTimer = new Timer(OnTimeout, null, timeout, Timeout.Infinite);
    }

    private void OnTimeout(object? state)
    {
        if (_disposed || _session == null || !_session.IsOpen) return;

        _logger.LogWarning("Ping timeout for session {Sid}", _session.Sid);
        _session.CloseAsync().ConfigureAwait(false);
    }
}
