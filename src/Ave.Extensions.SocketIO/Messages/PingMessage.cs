namespace Ave.Extensions.SocketIO.Messages;

/// <summary>
/// Represents a ping keep-alive message.
/// </summary>
public class PingMessage : IMessage
{
    /// <inheritdoc />
    public MessageType Type => MessageType.Ping;
}
