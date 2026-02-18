using Ave.Extensions.SocketIO.Client.Session.EngineIOAdapter;

namespace Ave.Extensions.SocketIO.Client.Session.WebSocket.EngineIOAdapter;

/// <summary>
/// Defines a WebSocket-specific Engine.IO adapter.
/// </summary>
public interface IWebSocketEngineIOAdapter : IEngineIOAdapter
{
    /// <summary>
    /// Writes a protocol frame around the binary data.
    /// </summary>
    byte[] WriteProtocolFrame(byte[] bytes);

    /// <summary>
    /// Reads the payload from a protocol frame.
    /// </summary>
    byte[] ReadProtocolFrame(byte[] bytes);
}
