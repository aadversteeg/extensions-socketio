namespace Ave.Extensions.SocketIO.Serialization.Decapsulation;

/// <summary>
/// Result of parsing a binary event or ack message, including the expected binary attachment count.
/// </summary>
public class BinaryEventMessageResult : MessageResult
{
    /// <summary>
    /// Gets or sets the number of binary attachments expected.
    /// </summary>
    public int BytesCount { get; set; }
}
