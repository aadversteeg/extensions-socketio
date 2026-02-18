using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Ave.Extensions.SocketIO.Messages;

namespace Ave.Extensions.SocketIO.Serialization.NewtonsoftJson
{
    /// <summary>
    /// Defines a Newtonsoft.Json-based acknowledgement message.
    /// </summary>
    public interface INewtonJsonAckMessage : IDataMessage
    {
        /// <summary>
        /// Gets or sets the parsed data items.
        /// </summary>
        JArray DataItems { get; set; }

        /// <summary>
        /// Gets or sets the JSON serializer settings.
        /// </summary>
        JsonSerializerSettings JsonSerializerSettings { get; set; }
    }
}
