using Ave.Extensions.SocketIO.Messages;

namespace Ave.Extensions.SocketIO.Serialization;

/// <summary>
/// Defines Engine.IO version-specific message deserialization.
/// </summary>
public interface IEngineIOMessageAdapter
{
    /// <summary>
    /// Deserializes a connected message from the given text.
    /// </summary>
    ConnectedMessage DeserializeConnectedMessage(string text);

    /// <summary>
    /// Deserializes an error message from the given text.
    /// </summary>
    ErrorMessage DeserializeErrorMessage(string text);
}
