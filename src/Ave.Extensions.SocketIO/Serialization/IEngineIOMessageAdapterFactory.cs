namespace Ave.Extensions.SocketIO.Serialization;

/// <summary>
/// Factory for creating Engine.IO message adapters based on protocol version.
/// </summary>
public interface IEngineIOMessageAdapterFactory
{
    /// <summary>
    /// Creates an <see cref="IEngineIOMessageAdapter"/> for the specified Engine.IO version.
    /// </summary>
    IEngineIOMessageAdapter Create(EngineIOVersion engineIOVersion);
}
