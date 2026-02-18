using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Ave.Extensions.SocketIO.Messages;

namespace Ave.Extensions.SocketIO.Serialization.NewtonsoftJson
{
    /// <summary>
    /// Acknowledgement message using Newtonsoft.Json for deserialization.
    /// </summary>
    public class NewtonJsonAckMessage : INewtonJsonAckMessage
    {
        /// <summary>
        /// Gets or sets the parsed data items.
        /// </summary>
        public JArray DataItems { get; set; } = null!;

        /// <inheritdoc />
        public virtual MessageType Type => MessageType.Ack;

        /// <inheritdoc />
        public string? Namespace { get; set; }

        /// <inheritdoc />
        public int Id { get; set; }

        /// <inheritdoc />
        public string RawText { get; set; } = null!;

        /// <summary>
        /// Gets or sets the JSON serializer settings.
        /// </summary>
        public JsonSerializerSettings JsonSerializerSettings { get; set; } = null!;

        /// <summary>
        /// Gets the settings for deserialization.
        /// </summary>
        protected virtual JsonSerializerSettings GetSettings()
        {
            return JsonSerializerSettings;
        }

        /// <inheritdoc />
        public virtual T? GetValue<T>(int index)
        {
            var settings = GetSettings();
            var serializer = JsonSerializer.Create(settings);
            return DataItems[index].ToObject<T>(serializer);
        }

        /// <inheritdoc />
        public virtual object? GetValue(Type type, int index)
        {
            var settings = GetSettings();
            var serializer = JsonSerializer.Create(settings);
            return DataItems[index].ToObject(type, serializer);
        }
    }
}
