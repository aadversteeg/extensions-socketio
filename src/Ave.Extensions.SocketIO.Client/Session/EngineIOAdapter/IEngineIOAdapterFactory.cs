namespace Ave.Extensions.SocketIO.Client.Session.EngineIOAdapter;

/// <summary>
/// Factory for creating Engine.IO adapters.
/// </summary>
public interface IEngineIOAdapterFactory
{
    /// <summary>
    /// Creates an Engine.IO adapter for the specified compatibility level.
    /// </summary>
    T Create<T>(EngineIOCompatibility compatibility) where T : IEngineIOAdapter;
}
