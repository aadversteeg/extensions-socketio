using System.Collections.Generic;
using Ave.Extensions.SocketIO.Protocol;

namespace Ave.Extensions.SocketIO.Server.EngineIO.Codec;

/// <summary>
/// Encodes and decodes Engine.IO multi-packet payloads for HTTP polling transport.
/// </summary>
public interface IPayloadCodec
{
    /// <summary>
    /// Encodes multiple protocol messages into a single payload string.
    /// </summary>
    string Encode(IReadOnlyList<ProtocolMessage> messages);

    /// <summary>
    /// Decodes a text payload into individual protocol messages.
    /// </summary>
    IEnumerable<ProtocolMessage> Decode(string text);

    /// <summary>
    /// Decodes a binary payload into individual protocol messages.
    /// </summary>
    IEnumerable<ProtocolMessage> DecodeBytes(byte[] bytes);
}
