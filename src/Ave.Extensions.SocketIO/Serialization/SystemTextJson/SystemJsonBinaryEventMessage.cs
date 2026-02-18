using Ave.Extensions.SocketIO.Messages;

namespace Ave.Extensions.SocketIO.Serialization.SystemTextJson;

/// <summary>
/// System.Text.Json implementation of a binary event message.
/// </summary>
public class SystemJsonBinaryEventMessage : SystemJsonBinaryAckMessage, ISystemJsonEventMessage
{
    /// <inheritdoc />
    public override MessageType Type => MessageType.Binary;

    /// <summary>
    /// Gets or sets the event name.
    /// </summary>
    public string Event { get; set; } = null!;
}
