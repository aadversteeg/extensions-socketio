using System.Collections.Generic;
using Ave.Extensions.SocketIO.Messages;
using Ave.Extensions.SocketIO.Protocol;

namespace Ave.Extensions.SocketIO.Serialization;

/// <summary>
/// Defines the contract for serializing and deserializing Socket.IO messages.
/// </summary>
public interface ISerializer
{
    /// <summary>
    /// Gets or sets the namespace for message serialization.
    /// </summary>
    string? Namespace { get; set; }

    /// <summary>
    /// Serializes an object to a JSON string.
    /// </summary>
    string Serialize(object data);

    /// <summary>
    /// Serializes event data to a list of protocol messages.
    /// </summary>
    List<ProtocolMessage> Serialize(object[] data);

    /// <summary>
    /// Serializes event data with a packet identifier for acknowledgement tracking.
    /// </summary>
    List<ProtocolMessage> Serialize(object[] data, int packetId);

    /// <summary>
    /// Serializes acknowledgement response data.
    /// </summary>
    List<ProtocolMessage> SerializeAckData(object[] data, int packetId);

    /// <summary>
    /// Deserializes a raw text string into a Socket.IO message.
    /// </summary>
    IMessage? Deserialize(string text);

    /// <summary>
    /// Sets the Engine.IO message adapter for protocol-version-specific deserialization.
    /// </summary>
    void SetEngineIOMessageAdapter(IEngineIOMessageAdapter adapter);
}
