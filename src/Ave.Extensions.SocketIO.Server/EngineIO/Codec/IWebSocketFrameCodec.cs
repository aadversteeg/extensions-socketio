namespace Ave.Extensions.SocketIO.Server.EngineIO.Codec;

/// <summary>
/// Encodes and decodes binary frames for WebSocket transport.
/// </summary>
public interface IWebSocketFrameCodec
{
    /// <summary>
    /// Wraps binary data in a protocol frame for sending.
    /// </summary>
    byte[] WriteFrame(byte[] bytes);

    /// <summary>
    /// Extracts binary data from a received protocol frame.
    /// </summary>
    byte[] ReadFrame(byte[] bytes);
}
