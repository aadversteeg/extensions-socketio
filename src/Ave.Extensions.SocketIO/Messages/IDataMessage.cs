using System;

namespace Ave.Extensions.SocketIO.Messages;

/// <summary>
/// Represents a message that carries data with typed access.
/// </summary>
public interface IDataMessage : IMessage
{
    /// <summary>
    /// Gets or sets the namespace for the message.
    /// </summary>
    string? Namespace { get; set; }

    /// <summary>
    /// Gets or sets the packet identifier used for acknowledgements.
    /// </summary>
    int Id { get; set; }

    /// <summary>
    /// Gets or sets the raw text of the message data.
    /// </summary>
    string RawText { get; set; }

    /// <summary>
    /// Gets a typed value from the message data at the specified index.
    /// </summary>
    T? GetValue<T>(int index);

    /// <summary>
    /// Gets a value of the specified type from the message data at the specified index.
    /// </summary>
    object? GetValue(Type type, int index);
}
