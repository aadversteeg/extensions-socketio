using System;

namespace Ave.Extensions.SocketIO.Server.EngineIO.Codec;

/// <summary>
/// Engine.IO v3 WebSocket frame codec. Binary frames have a leading 0x04 prefix byte.
/// </summary>
public class WebSocketFrameCodecV3 : IWebSocketFrameCodec
{
    /// <inheritdoc />
    public byte[] WriteFrame(byte[] bytes)
    {
        var buffer = new byte[bytes.Length + 1];
        buffer[0] = 4;
        Buffer.BlockCopy(bytes, 0, buffer, 1, bytes.Length);
        return buffer;
    }

    /// <inheritdoc />
    public byte[] ReadFrame(byte[] bytes)
    {
        var result = new byte[bytes.Length - 1];
        Buffer.BlockCopy(bytes, 1, result, 0, result.Length);
        return result;
    }
}
