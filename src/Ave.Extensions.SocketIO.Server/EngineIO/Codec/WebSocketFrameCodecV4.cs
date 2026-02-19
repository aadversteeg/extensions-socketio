namespace Ave.Extensions.SocketIO.Server.EngineIO.Codec;

/// <summary>
/// Engine.IO v4 WebSocket frame codec. Binary frames are passed through without framing.
/// </summary>
public class WebSocketFrameCodecV4 : IWebSocketFrameCodec
{
    /// <inheritdoc />
    public byte[] WriteFrame(byte[] bytes)
    {
        return bytes;
    }

    /// <inheritdoc />
    public byte[] ReadFrame(byte[] bytes)
    {
        return bytes;
    }
}
