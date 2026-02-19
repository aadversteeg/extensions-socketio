namespace Ave.Extensions.SocketIO.Server.EngineIO;

/// <summary>
/// Thread-safe store for Engine.IO sessions.
/// </summary>
public interface IEngineIOSessionStore
{
    /// <summary>
    /// Creates a new session with the specified version and transport.
    /// </summary>
    IEngineIOSession Create(EngineIOVersion version, TransportProtocol transport);

    /// <summary>
    /// Gets a session by its identifier, or null if not found.
    /// </summary>
    IEngineIOSession? Get(string sid);

    /// <summary>
    /// Removes a session by its identifier.
    /// </summary>
    bool Remove(string sid);

    /// <summary>
    /// Gets the number of active sessions.
    /// </summary>
    int Count { get; }
}
