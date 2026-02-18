using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using Ave.Extensions.SocketIO.Messages;

namespace Ave.Extensions.SocketIO.Serialization.SystemTextJson;

/// <summary>
/// System.Text.Json implementation of an acknowledgement message.
/// </summary>
public class SystemJsonAckMessage : ISystemJsonAckMessage
{
    /// <summary>
    /// Gets or sets the JSON array of data items.
    /// </summary>
    public JsonArray DataItems { get; set; } = null!;

    /// <inheritdoc />
    public virtual MessageType Type => MessageType.Ack;

    /// <inheritdoc />
    public string? Namespace { get; set; }

    /// <inheritdoc />
    public int Id { get; set; }

    /// <inheritdoc />
    public string RawText { get; set; } = null!;

    /// <summary>
    /// Gets or sets the JSON serializer options.
    /// </summary>
    public JsonSerializerOptions JsonSerializerOptions { get; set; } = null!;

    /// <summary>
    /// Gets the JSON serializer options to use for deserialization.
    /// </summary>
    protected virtual JsonSerializerOptions GetOptions()
    {
        return JsonSerializerOptions;
    }

    /// <inheritdoc />
    public virtual T? GetValue<T>(int index)
    {
        var options = GetOptions();
        return DataItems[index]!.Deserialize<T>(options);
    }

    /// <inheritdoc />
    public virtual object? GetValue(Type type, int index)
    {
        var options = GetOptions();
        return DataItems[index]!.Deserialize(type, options);
    }
}
