using System.Text.Json;
using System.Text.Json.Nodes;
using Ave.Extensions.SocketIO.Messages;

namespace Ave.Extensions.SocketIO.Serialization.SystemTextJson;

/// <summary>
/// Engine.IO v3 message adapter using System.Text.Json.
/// </summary>
public class SystemJsonEngineIO3MessageAdapter : IEngineIOMessageAdapter
{
    /// <inheritdoc />
    public ConnectedMessage DeserializeConnectedMessage(string text)
    {
        var message = new ConnectedMessage();
        if (!string.IsNullOrEmpty(text))
        {
            message.Namespace = text.TrimEnd(',');
        }
        return message;
    }

    /// <inheritdoc />
    public ErrorMessage DeserializeErrorMessage(string text)
    {
        var error = JsonNode.Parse(text)!.Deserialize<string>()!;
        return new ErrorMessage
        {
            Error = error,
        };
    }
}
