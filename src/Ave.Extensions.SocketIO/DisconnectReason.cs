namespace Ave.Extensions.SocketIO;

/// <summary>
/// Constants for Socket.IO disconnect reasons.
/// </summary>
public static class DisconnectReason
{
    /// <summary>
    /// The server forcefully disconnected the client.
    /// </summary>
    public const string IOServerDisconnect = "io server disconnect";

    /// <summary>
    /// The client intentionally disconnected.
    /// </summary>
    public const string IOClientDisconnect = "io client disconnect";

    /// <summary>
    /// The connection was lost due to ping timeout.
    /// </summary>
    public const string PingTimeout = "ping timeout";

    /// <summary>
    /// The transport was closed.
    /// </summary>
    public const string TransportClose = "transport close";

    /// <summary>
    /// A transport error occurred.
    /// </summary>
    public const string TransportError = "transport error";
}
