using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ave.Extensions.SocketIO.Messages;
using Ave.Extensions.SocketIO.Protocol;
using Ave.Extensions.SocketIO.Serialization.Decapsulation;

namespace Ave.Extensions.SocketIO.Serialization;

/// <summary>
/// Base class for JSON-based Socket.IO serializers, handling protocol framing and message routing.
/// </summary>
public abstract class BaseJsonSerializer : ISerializer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BaseJsonSerializer"/> class.
    /// </summary>
    protected BaseJsonSerializer(IDecapsulable decapsulator)
    {
        Decapsulator = decapsulator;
    }

    /// <summary>
    /// Gets the decapsulator used for parsing raw protocol text.
    /// </summary>
    protected IDecapsulable Decapsulator { get; }

    /// <summary>
    /// Gets the Engine.IO message adapter for protocol-version-specific deserialization.
    /// </summary>
    protected IEngineIOMessageAdapter EngineIOMessageAdapter { get; private set; } = null!;

    /// <inheritdoc />
    public string? Namespace { get; set; }

    /// <summary>
    /// Serializes the data array into JSON and extracts binary attachments.
    /// </summary>
    protected abstract SerializationResult SerializeCore(object[] data);

    /// <inheritdoc />
    public abstract string Serialize(object data);

    /// <inheritdoc />
    public void SetEngineIOMessageAdapter(IEngineIOMessageAdapter adapter)
    {
        EngineIOMessageAdapter = adapter;
    }

    private static void ThrowIfDataIsInvalid(object[] data)
    {
        if (data is null)
        {
            throw new ArgumentNullException(nameof(data));
        }
        if (data.Length == 0)
        {
            throw new ArgumentException("Argument must contain at least 1 item", nameof(data));
        }
    }

    private static StringBuilder NewStringBuilder(int jsonLength)
    {
        return new StringBuilder(jsonLength + 16);
    }

    private void AddPrefix(StringBuilder builder, int bytesCount)
    {
        if (bytesCount == 0)
        {
            builder.Append("42");
        }
        else
        {
            builder.Append("45").Append(bytesCount).Append('-');
        }

        if (!string.IsNullOrEmpty(Namespace))
        {
            builder.Append(Namespace).Append(',');
        }
    }

    private void AddAckPrefix(StringBuilder builder, int bytesCount)
    {
        if (bytesCount == 0)
        {
            builder.Append("43");
        }
        else
        {
            builder.Append("46").Append(bytesCount).Append('-');
        }

        if (!string.IsNullOrEmpty(Namespace))
        {
            builder.Append(Namespace).Append(',');
        }
    }

    /// <inheritdoc />
    public List<ProtocolMessage> Serialize(object[] data)
    {
        ThrowIfDataIsInvalid(data);
        var result = SerializeCore(data);
        var builder = NewStringBuilder(result.Json.Length);
        AddPrefix(builder, result.Bytes.Count);
        builder.Append(result.Json);
        return GetSerializeResult(builder.ToString(), result.Bytes);
    }

    private List<ProtocolMessage> Serialize(object[] data, int packetId, Action<StringBuilder, int> prefixHandler)
    {
        ThrowIfDataIsInvalid(data);
        var result = SerializeCore(data);
        var builder = NewStringBuilder(result.Json.Length);
        prefixHandler(builder, result.Bytes.Count);
        builder.Append(packetId);
        builder.Append(result.Json);
        return GetSerializeResult(builder.ToString(), result.Bytes);
    }

    /// <inheritdoc />
    public List<ProtocolMessage> Serialize(object[] data, int packetId)
    {
        return Serialize(data, packetId, AddPrefix);
    }

    /// <inheritdoc />
    public List<ProtocolMessage> SerializeAckData(object[] data, int packetId)
    {
        return Serialize(data, packetId, AddAckPrefix);
    }

    /// <inheritdoc />
    public IMessage? Deserialize(string text)
    {
        var result = Decapsulator.DecapsulateRawText(text);
        if (!result.Success)
        {
            return null;
        }

        return NewMessage(result.Type!.Value, result.Data);
    }

    /// <summary>
    /// Creates a new message of the specified type from the deserialized text.
    /// </summary>
    protected abstract IMessage NewMessage(MessageType type, string text);

    private static List<ProtocolMessage> GetSerializeResult(string text, IEnumerable<byte[]> bytes)
    {
        var list = new List<ProtocolMessage>
        {
            new ProtocolMessage
            {
                Type = ProtocolMessageType.Text,
                Text = text,
            },
        };
        var byteMessages = bytes.Select(item => new ProtocolMessage
        {
            Type = ProtocolMessageType.Bytes,
            Bytes = item,
        });
        list.AddRange(byteMessages);
        return list;
    }
}
