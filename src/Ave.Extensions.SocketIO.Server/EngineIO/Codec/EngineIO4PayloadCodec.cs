using System;
using System.Collections.Generic;
using System.Text;
using Ave.Extensions.SocketIO.Protocol;

namespace Ave.Extensions.SocketIO.Server.EngineIO.Codec;

/// <summary>
/// Engine.IO v4 payload codec using record separator (\x1E) delimiter
/// and b-prefix base64 encoding for binary data.
/// </summary>
public class EngineIO4PayloadCodec : IPayloadCodec
{
    private const string Delimiter = "\u001E";

    /// <inheritdoc />
    public string Encode(IReadOnlyList<ProtocolMessage> messages)
    {
        if (messages.Count == 0) return string.Empty;

        var builder = new StringBuilder();
        for (int i = 0; i < messages.Count; i++)
        {
            if (i > 0) builder.Append(Delimiter);

            var msg = messages[i];
            if (msg.Type == ProtocolMessageType.Bytes && msg.Bytes != null)
            {
                builder.Append('b').Append(Convert.ToBase64String(msg.Bytes));
            }
            else
            {
                builder.Append(msg.Text);
            }
        }
        return builder.ToString();
    }

    /// <inheritdoc />
    public IEnumerable<ProtocolMessage> Decode(string text)
    {
        var items = text.Split(new[] { Delimiter }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var item in items)
        {
            if (item.Length > 0 && item[0] == 'b')
            {
                var bytes = Convert.FromBase64String(item.Substring(1));
                yield return new ProtocolMessage
                {
                    Type = ProtocolMessageType.Bytes,
                    Bytes = bytes,
                };
            }
            else
            {
                yield return new ProtocolMessage
                {
                    Type = ProtocolMessageType.Text,
                    Text = item,
                };
            }
        }
    }

    /// <inheritdoc />
    public IEnumerable<ProtocolMessage> DecodeBytes(byte[] bytes)
    {
        // EIO v4 polling does not use binary-frame payloads; all binary is base64 in text
        return Array.Empty<ProtocolMessage>();
    }
}
