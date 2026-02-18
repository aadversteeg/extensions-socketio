namespace Ave.Extensions.SocketIO;

/// <summary>
/// Specifies the transport protocol used for Socket.IO communication.
/// </summary>
public enum TransportProtocol
{
    /// <summary>
    /// HTTP long-polling transport.
    /// </summary>
    Polling,

    /// <summary>
    /// WebSocket transport.
    /// </summary>
    WebSocket,
}
