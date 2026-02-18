namespace Ave.Extensions.SocketIO.Messages;

/// <summary>
/// Represents a Socket.IO protocol message.
/// </summary>
public interface IMessage
{
    /// <summary>
    /// Gets the message type.
    /// </summary>
    MessageType Type { get; }
}
