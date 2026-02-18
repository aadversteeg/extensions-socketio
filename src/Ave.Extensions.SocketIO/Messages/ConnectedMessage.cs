namespace Ave.Extensions.SocketIO.Messages;

/// <summary>
/// Represents a Socket.IO connected message with session and namespace information.
/// </summary>
public class ConnectedMessage : INamespaceMessage
{
    /// <inheritdoc />
    public MessageType Type => MessageType.Connected;

    /// <inheritdoc />
    public string? Namespace { get; set; }

    /// <summary>
    /// Gets or sets the session identifier.
    /// </summary>
    public string? Sid { get; set; }
}
