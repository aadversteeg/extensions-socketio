using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Ave.Extensions.SocketIO.Serialization.NewtonsoftJson
{
    /// <summary>
    /// Converts byte arrays to/from Socket.IO binary placeholder format.
    /// </summary>
    public class ByteArrayConverter : JsonConverter
    {
        private const string Placeholder = "_placeholder";
        private const string Num = "num";

        /// <summary>
        /// Gets or sets the collected byte arrays.
        /// </summary>
        public IList<byte[]> Bytes { get; set; } = new List<byte[]>();

        /// <inheritdoc />
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value == null)
                return;
            Bytes.Add((byte[])value);
            writer.WriteStartObject();
            writer.WritePropertyName(Placeholder);
            writer.WriteValue(true);
            writer.WritePropertyName(Num);
            writer.WriteValue(Bytes.Count - 1);
            writer.WriteEndObject();
        }

        /// <inheritdoc />
        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.StartObject)
                return null;
            reader.Read();
            if (reader.TokenType != JsonToken.PropertyName || reader.Value?.ToString() != Placeholder)
                return null;
            reader.Read();
            if (reader.TokenType != JsonToken.Boolean || !(bool)reader.Value)
                return null;
            reader.Read();
            if (reader.TokenType != JsonToken.PropertyName || reader.Value?.ToString() != Num)
                return null;
            reader.Read();
            if (reader.Value == null)
                return null;
            if (!int.TryParse(reader.Value.ToString(), out var num))
                return null;
            var bytes = Bytes[num];
            reader.Read();
            return bytes;
        }

        /// <inheritdoc />
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(byte[]);
        }
    }
}
