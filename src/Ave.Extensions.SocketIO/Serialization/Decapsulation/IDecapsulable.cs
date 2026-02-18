namespace Ave.Extensions.SocketIO.Serialization.Decapsulation;

/// <summary>
/// Defines methods for parsing raw Socket.IO protocol text into structured results.
/// </summary>
public interface IDecapsulable
{
    /// <summary>
    /// Parses the message type prefix from the raw text.
    /// </summary>
    DecapsulationResult DecapsulateRawText(string text);

    /// <summary>
    /// Parses namespace, packet id, and data from an event or ack message.
    /// </summary>
    MessageResult DecapsulateEventMessage(string text);

    /// <summary>
    /// Parses binary attachment count, namespace, packet id, and data from a binary event or ack message.
    /// </summary>
    BinaryEventMessageResult DecapsulateBinaryEventMessage(string text);
}
