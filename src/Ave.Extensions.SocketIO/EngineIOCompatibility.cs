namespace Ave.Extensions.SocketIO;

/// <summary>
/// Identifies the combination of transport and Engine.IO version for keyed service resolution.
/// </summary>
public enum EngineIOCompatibility
{
    /// <summary>
    /// HTTP polling with Engine.IO v3.
    /// </summary>
    HttpEngineIO3,

    /// <summary>
    /// HTTP polling with Engine.IO v4.
    /// </summary>
    HttpEngineIO4,

    /// <summary>
    /// WebSocket with Engine.IO v3.
    /// </summary>
    WebSocketEngineIO3,

    /// <summary>
    /// WebSocket with Engine.IO v4.
    /// </summary>
    WebSocketEngineIO4,
}
