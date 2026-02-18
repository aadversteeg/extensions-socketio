using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ave.Extensions.SocketIO.Serialization.SystemTextJson;

/// <summary>
/// JSON converter that handles byte array serialization as Socket.IO binary placeholders.
/// </summary>
public class ByteArrayConverter : JsonConverter<byte[]>
{
    private const string Placeholder = "_placeholder";
    private const string Num = "num";

    /// <summary>
    /// Gets or sets the collection of binary data extracted during serialization or provided for deserialization.
    /// </summary>
    public IList<byte[]> Bytes { get; set; } = new List<byte[]>();

    /// <inheritdoc />
    public override byte[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject) return null!;
        reader.Read();
        if (reader.TokenType != JsonTokenType.PropertyName || reader.GetString() != Placeholder) return null!;
        reader.Read();
        if (reader.TokenType != JsonTokenType.True || !reader.GetBoolean()) return null!;
        reader.Read();
        if (reader.TokenType != JsonTokenType.PropertyName || reader.GetString() != Num) return null!;
        reader.Read();
        var num = reader.GetInt32();
        var bytes = Bytes[num];
        reader.Read();
        return bytes;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, byte[] value, JsonSerializerOptions options)
    {
        Bytes.Add(value);
        writer.WriteStartObject();
        writer.WritePropertyName(Placeholder);
        writer.WriteBooleanValue(true);
        writer.WritePropertyName(Num);
        writer.WriteNumberValue(Bytes.Count - 1);
        writer.WriteEndObject();
    }
}
