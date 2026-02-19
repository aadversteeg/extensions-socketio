using System;

namespace Ave.Extensions.SocketIO.Server.EngineIO;

/// <summary>
/// Manages heartbeat (ping/pong) for an Engine.IO session.
/// </summary>
public interface IHeartbeatManager : IDisposable
{
    /// <summary>
    /// Starts heartbeat monitoring for the specified session.
    /// </summary>
    void Start(IEngineIOSession session);

    /// <summary>
    /// Handles a ping message received from the client.
    /// </summary>
    void HandlePing();

    /// <summary>
    /// Handles a pong message received from the client.
    /// </summary>
    void HandlePong();
}
