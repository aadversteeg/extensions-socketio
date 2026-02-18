using System.Collections.Generic;
using Ave.Extensions.SocketIO.Client.Protocol.Http;
using Ave.Extensions.SocketIO.Client.Session.EngineIOAdapter;
using Ave.Extensions.SocketIO.Protocol;

namespace Ave.Extensions.SocketIO.Client.Session.Http.EngineIOAdapter;

/// <summary>
/// Defines an HTTP-specific Engine.IO adapter.
/// </summary>
public interface IHttpEngineIOAdapter : IEngineIOAdapter
{
    /// <summary>
    /// Creates an HTTP request from binary data.
    /// </summary>
    HttpRequest ToHttpRequest(ICollection<byte[]> bytes);

    /// <summary>
    /// Creates an HTTP request from text content.
    /// </summary>
    HttpRequest ToHttpRequest(string content);

    /// <summary>
    /// Extracts protocol messages from a text response.
    /// </summary>
    IEnumerable<ProtocolMessage> ExtractMessagesFromText(string text);

    /// <summary>
    /// Extracts protocol messages from a binary response.
    /// </summary>
    IEnumerable<ProtocolMessage> ExtractMessagesFromBytes(byte[] bytes);
}
