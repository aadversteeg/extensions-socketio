namespace Ave.Extensions.SocketIO.Messages;

/// <summary>
/// Represents a Socket.IO event message with an event name and data.
/// </summary>
public interface IEventMessage : IDataMessage
{
    /// <summary>
    /// Gets or sets the event name.
    /// </summary>
    string Event { get; set; }
}
