using System.Collections.Generic;
using Newtonsoft.Json;
using Ave.Extensions.SocketIO.Messages;

namespace Ave.Extensions.SocketIO.Serialization.NewtonsoftJson
{
    /// <summary>
    /// Binary acknowledgement message using Newtonsoft.Json for deserialization.
    /// </summary>
    public class NewtonJsonBinaryAckMessage : NewtonJsonAckMessage, IBinaryAckMessage
    {
        /// <inheritdoc />
        public override MessageType Type => MessageType.BinaryAck;

        /// <inheritdoc />
        public IList<byte[]> Bytes { get; set; } = null!;

        /// <inheritdoc />
        public int BytesCount { get; set; }

        /// <inheritdoc />
        protected override JsonSerializerSettings GetSettings()
        {
            var settings = new JsonSerializerSettings(JsonSerializerSettings);
            var converter = new ByteArrayConverter
            {
                Bytes = Bytes,
            };
            settings.Converters.Add(converter);
            return settings;
        }

        /// <inheritdoc />
        public bool ReadyDelivery
        {
            get
            {
                if (Bytes is null)
                {
                    return false;
                }
                return BytesCount == Bytes.Count;
            }
        }

        /// <inheritdoc />
        public void Add(byte[] bytes)
        {
            if (Bytes == null)
            {
                Bytes = new List<byte[]>(BytesCount);
            }
            Bytes.Add(bytes);
        }
    }
}
