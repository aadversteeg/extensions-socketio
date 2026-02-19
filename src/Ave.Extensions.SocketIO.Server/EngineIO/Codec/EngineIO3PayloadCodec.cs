using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ave.Extensions.SocketIO.Protocol;

namespace Ave.Extensions.SocketIO.Server.EngineIO.Codec;

/// <summary>
/// Engine.IO v3 payload codec using length-prefix framing for text
/// and binary byte-length framing for binary data.
/// </summary>
public class EngineIO3PayloadCodec : IPayloadCodec
{
    /// <inheritdoc />
    public string Encode(IReadOnlyList<ProtocolMessage> messages)
    {
        if (messages.Count == 0) return string.Empty;

        // If all messages are text, use simple length:content framing
        if (messages.All(m => m.Type == ProtocolMessageType.Text))
        {
            var builder = new StringBuilder();
            foreach (var msg in messages)
            {
                var text = msg.Text ?? string.Empty;
                builder.Append(text.Length).Append(':').Append(text);
            }
            return builder.ToString();
        }

        // Mixed text and binary requires byte-level framing
        var result = new StringBuilder();
        foreach (var msg in messages)
        {
            if (msg.Type == ProtocolMessageType.Text)
            {
                var text = msg.Text ?? string.Empty;
                result.Append(text.Length).Append(':').Append(text);
            }
            else
            {
                // Binary in text payload: base64 encode with b prefix
                var b64 = Convert.ToBase64String(msg.Bytes ?? Array.Empty<byte>());
                var content = "b" + b64;
                result.Append(content.Length).Append(':').Append(content);
            }
        }
        return result.ToString();
    }

    /// <inheritdoc />
    public IEnumerable<ProtocolMessage> Decode(string text)
    {
        var p = 0;
        while (p < text.Length)
        {
            var index = text.IndexOf(':', p);
            if (index == -1) break;

            var lengthStr = text.Substring(p, index - p);
            if (!int.TryParse(lengthStr, out var length)) break;

            var msg = text.Substring(index + 1, length);
            yield return new ProtocolMessage
            {
                Type = ProtocolMessageType.Text,
                Text = msg,
            };

            p = index + length + 1;
        }
    }

    /// <inheritdoc />
    public IEnumerable<ProtocolMessage> DecodeBytes(byte[] bytes)
    {
        var index = 0;
        while (index < bytes.Length)
        {
            byte messageType = bytes[index];
            index++;
            var payloadLength = 0;

            while (index < bytes.Length && bytes[index] != byte.MaxValue)
            {
                payloadLength = payloadLength * 10 + bytes[index++];
            }

            index++; // skip 0xFF separator

            if (messageType == byte.MinValue)
            {
                // Text message
                var data = new byte[payloadLength];
                Buffer.BlockCopy(bytes, index, data, 0, data.Length);
                yield return new ProtocolMessage
                {
                    Type = ProtocolMessageType.Text,
                    Text = Encoding.UTF8.GetString(data),
                };
            }
            else
            {
                // Binary message (skip leading 0x04 byte)
                var data = new byte[payloadLength - 1];
                Buffer.BlockCopy(bytes, index + 1, data, 0, data.Length);
                yield return new ProtocolMessage
                {
                    Type = ProtocolMessageType.Bytes,
                    Bytes = data,
                };
            }

            index += payloadLength;
        }
    }
}
