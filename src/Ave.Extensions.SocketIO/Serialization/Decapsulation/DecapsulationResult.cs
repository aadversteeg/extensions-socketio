using Ave.Extensions.SocketIO.Messages;

namespace Ave.Extensions.SocketIO.Serialization.Decapsulation;

/// <summary>
/// Result of parsing the message type prefix from raw protocol text.
/// </summary>
public class DecapsulationResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the decapsulation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the parsed message type.
    /// </summary>
    public MessageType? Type { get; set; }

    /// <summary>
    /// Gets or sets the remaining data after the message type prefix.
    /// </summary>
    public string Data { get; set; } = null!;
}
