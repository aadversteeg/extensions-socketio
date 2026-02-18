namespace Ave.Extensions.SocketIO;

/// <summary>
/// Specifies the Engine.IO protocol version.
/// </summary>
public enum EngineIOVersion
{
    /// <summary>
    /// Engine.IO protocol version 3, compatible with Socket.IO 2.x.
    /// </summary>
    V3 = 3,

    /// <summary>
    /// Engine.IO protocol version 4, compatible with Socket.IO 4.x/5.x.
    /// </summary>
    V4 = 4,
}
