using System.Text.Json;
using System.Text.Json.Nodes;
using Ave.Extensions.SocketIO.Messages;

namespace Ave.Extensions.SocketIO.Serialization.SystemTextJson;

/// <summary>
/// Internal interface for System.Text.Json acknowledgement message implementation.
/// </summary>
public interface ISystemJsonAckMessage : IDataMessage
{
    /// <summary>
    /// Gets or sets the JSON array of data items.
    /// </summary>
    JsonArray DataItems { get; set; }

    /// <summary>
    /// Gets or sets the JSON serializer options used for deserialization.
    /// </summary>
    JsonSerializerOptions JsonSerializerOptions { get; set; }
}
