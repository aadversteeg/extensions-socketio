using System;

namespace Ave.Extensions.SocketIO.Messages;

/// <summary>
/// Represents a pong response message with round-trip duration.
/// </summary>
public class PongMessage : IMessage
{
    /// <inheritdoc />
    public MessageType Type => MessageType.Pong;

    /// <summary>
    /// Gets or sets the round-trip duration.
    /// </summary>
    public TimeSpan Duration { get; set; }
}
