using Ave.Extensions.SocketIO.Messages;

namespace Ave.Extensions.SocketIO.Serialization.SystemTextJson;

/// <summary>
/// System.Text.Json implementation of an event message.
/// </summary>
public class SystemJsonEventMessage : SystemJsonAckMessage, ISystemJsonEventMessage
{
    /// <inheritdoc />
    public override MessageType Type => MessageType.Event;

    /// <summary>
    /// Gets or sets the event name.
    /// </summary>
    public string Event { get; set; } = null!;
}
