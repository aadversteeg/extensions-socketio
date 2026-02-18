namespace Ave.Extensions.SocketIO.Client.Protocol.WebSocket;

/// <summary>
/// WebSocket message type.
/// </summary>
public enum WebSocketMessageType
{
    /// <summary>Text message.</summary>
    Text,
    /// <summary>Binary message.</summary>
    Binary,
    /// <summary>Close message.</summary>
    Close
}

/// <summary>
/// Represents a WebSocket message.
/// </summary>
public class WebSocketMessage
{
    /// <summary>
    /// Gets or sets the message type.
    /// </summary>
    public WebSocketMessageType Type { get; set; }

    /// <summary>
    /// Gets or sets the message bytes.
    /// </summary>
    public byte[] Bytes { get; set; } = null!;
}
