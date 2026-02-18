using System.Collections.Generic;

namespace Ave.Extensions.SocketIO.Messages;

/// <summary>
/// Represents the Engine.IO handshake (opened) message containing session information.
/// </summary>
public class OpenedMessage : IMessage
{
    /// <inheritdoc />
    public MessageType Type => MessageType.Opened;

    /// <summary>
    /// Gets or sets the session identifier.
    /// </summary>
    public string Sid { get; set; } = null!;

    /// <summary>
    /// Gets or sets the ping interval in milliseconds.
    /// </summary>
    public int PingInterval { get; set; }

    /// <summary>
    /// Gets or sets the ping timeout in milliseconds.
    /// </summary>
    public int PingTimeout { get; set; }

    /// <summary>
    /// Gets or sets the maximum payload size in bytes.
    /// </summary>
    public int MaxPayload { get; set; }

    /// <summary>
    /// Gets or sets the list of available transport upgrades.
    /// </summary>
    public List<string> Upgrades { get; set; } = new List<string>();
}
