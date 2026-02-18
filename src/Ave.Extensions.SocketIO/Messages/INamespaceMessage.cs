namespace Ave.Extensions.SocketIO.Messages;

/// <summary>
/// Represents a message associated with a specific namespace.
/// </summary>
public interface INamespaceMessage : IMessage
{
    /// <summary>
    /// Gets or sets the namespace.
    /// </summary>
    string? Namespace { get; set; }
}
