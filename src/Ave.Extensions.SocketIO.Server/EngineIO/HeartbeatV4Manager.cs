using System;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Ave.Extensions.SocketIO.Server.EngineIO;

/// <summary>
/// Engine.IO v4 heartbeat manager. Server sends ping, expects client pong within timeout.
/// </summary>
public class HeartbeatV4Manager : IHeartbeatManager
{
    private readonly int _pingInterval;
    private readonly int _pingTimeout;
    private readonly ILogger<HeartbeatV4Manager> _logger;
    private Timer? _pingTimer;
    private Timer? _pongTimer;
    private IEngineIOSession? _session;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="HeartbeatV4Manager"/> class.
    /// </summary>
    public HeartbeatV4Manager(SocketIOServerOptions options, ILogger<HeartbeatV4Manager> logger)
    {
        _pingInterval = options.PingInterval;
        _pingTimeout = options.PingTimeout;
        _logger = logger;
    }

    /// <inheritdoc />
    public void Start(IEngineIOSession session)
    {
        _session = session;
        SchedulePing();
    }

    /// <inheritdoc />
    public void HandlePing()
    {
        // In v4, client doesn't send ping — this is a no-op
    }

    /// <inheritdoc />
    public void HandlePong()
    {
        _pongTimer?.Dispose();
        _pongTimer = null;
        SchedulePing();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _pingTimer?.Dispose();
        _pongTimer?.Dispose();
    }

    private void SchedulePing()
    {
        if (_disposed || _session == null || !_session.IsOpen) return;

        _pingTimer?.Dispose();
        _pingTimer = new Timer(SendPing, null, _pingInterval, Timeout.Infinite);
    }

    private void SendPing(object? state)
    {
        if (_disposed || _session == null || !_session.IsOpen) return;

        _logger.LogDebug("Sending ping to session {Sid}", _session.Sid);
        _session.SendAsync("2", CancellationToken.None).ConfigureAwait(false);

        // Start pong timeout
        _pongTimer?.Dispose();
        _pongTimer = new Timer(OnPongTimeout, null, _pingTimeout, Timeout.Infinite);
    }

    private void OnPongTimeout(object? state)
    {
        if (_disposed || _session == null || !_session.IsOpen) return;

        _logger.LogWarning("Pong timeout for session {Sid}", _session.Sid);
        _session.CloseAsync().ConfigureAwait(false);
    }
}
