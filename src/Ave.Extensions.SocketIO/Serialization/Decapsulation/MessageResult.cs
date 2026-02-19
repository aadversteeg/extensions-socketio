namespace Ave.Extensions.SocketIO.Serialization.Decapsulation;

/// <summary>
/// Result of parsing an event or ack message, containing namespace, packet id, and data.
/// </summary>
public class MessageResult
{
    /// <summary>
    /// Gets or sets the message namespace.
    /// </summary>
    public string? Namespace { get; set; }

    /// <summary>
    /// Gets or sets the packet identifier.
    /// </summary>
    public int Id { get; set; } = -1;

    /// <summary>
    /// Gets or sets the JSON data payload.
    /// </summary>
    public string Data { get; set; } = null!;
}
