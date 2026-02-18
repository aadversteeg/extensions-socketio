namespace Ave.Extensions.SocketIO.Protocol;

/// <summary>
/// Represents a low-level protocol message that can carry either text or binary data.
/// </summary>
public class ProtocolMessage
{
    /// <summary>
    /// Gets or sets the type of the protocol message.
    /// </summary>
    public ProtocolMessageType Type { get; set; }

    /// <summary>
    /// Gets or sets the text content. Used when <see cref="Type"/> is <see cref="ProtocolMessageType.Text"/>.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets the binary content. Used when <see cref="Type"/> is <see cref="ProtocolMessageType.Bytes"/>.
    /// </summary>
    public byte[]? Bytes { get; set; }
}
