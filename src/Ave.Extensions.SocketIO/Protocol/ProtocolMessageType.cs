namespace Ave.Extensions.SocketIO.Protocol;

/// <summary>
/// Specifies whether a protocol message contains text or binary data.
/// </summary>
public enum ProtocolMessageType
{
    /// <summary>
    /// Text message.
    /// </summary>
    Text,

    /// <summary>
    /// Binary message.
    /// </summary>
    Bytes,
}
