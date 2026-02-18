namespace Ave.Extensions.SocketIO.Messages;

/// <summary>
/// Represents a Socket.IO error message.
/// </summary>
public class ErrorMessage : INamespaceMessage
{
    /// <inheritdoc />
    public MessageType Type => MessageType.Error;

    /// <inheritdoc />
    public string? Namespace { get; set; }

    /// <summary>
    /// Gets or sets the error description.
    /// </summary>
    public string Error { get; set; } = null!;
}
