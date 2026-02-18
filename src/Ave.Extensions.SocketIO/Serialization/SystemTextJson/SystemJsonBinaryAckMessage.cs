using System.Collections.Generic;
using System.Text.Json;
using Ave.Extensions.SocketIO.Messages;

namespace Ave.Extensions.SocketIO.Serialization.SystemTextJson;

/// <summary>
/// System.Text.Json implementation of a binary acknowledgement message.
/// </summary>
public class SystemJsonBinaryAckMessage : SystemJsonAckMessage, IBinaryAckMessage
{
    /// <inheritdoc />
    public override MessageType Type => MessageType.BinaryAck;

    /// <summary>
    /// Gets or sets the binary attachments.
    /// </summary>
    public IList<byte[]> Bytes { get; set; } = new List<byte[]>();

    /// <summary>
    /// Gets or sets the expected number of binary attachments.
    /// </summary>
    public int BytesCount { get; set; }

    /// <inheritdoc />
    protected override JsonSerializerOptions GetOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerOptions);
        var converter = new ByteArrayConverter
        {
            Bytes = Bytes,
        };
        options.Converters.Add(converter);
        return options;
    }

    /// <inheritdoc />
    public bool ReadyDelivery => BytesCount == Bytes.Count;

    /// <inheritdoc />
    public void Add(byte[] bytes)
    {
        Bytes.Add(bytes);
    }
}
