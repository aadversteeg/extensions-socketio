namespace Ave.Extensions.SocketIO.Messages;

/// <summary>
/// Represents a Socket.IO disconnected message.
/// </summary>
public class DisconnectedMessage : IMessage
{
    /// <inheritdoc />
    public MessageType Type => MessageType.Disconnected;

    /// <summary>
    /// Gets or sets the namespace from which the disconnect occurred.
    /// </summary>
    public string? Namespace { get; set; }
}
