namespace Ave.Extensions.SocketIO.Messages;

/// <summary>
/// Represents a message that carries binary data attachments.
/// </summary>
public interface IBinaryMessage : IMessage
{
    /// <summary>
    /// Gets a value indicating whether all binary attachments have been received.
    /// </summary>
    bool ReadyDelivery { get; }

    /// <summary>
    /// Adds a binary attachment to the message.
    /// </summary>
    void Add(byte[] bytes);
}
