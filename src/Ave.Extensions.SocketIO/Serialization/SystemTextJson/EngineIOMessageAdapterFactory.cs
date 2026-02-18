using System;

namespace Ave.Extensions.SocketIO.Serialization.SystemTextJson;

/// <summary>
/// Factory that creates Engine.IO message adapters based on protocol version using System.Text.Json.
/// </summary>
public class EngineIOMessageAdapterFactory : IEngineIOMessageAdapterFactory
{
    private readonly Func<EngineIOVersion, IEngineIOMessageAdapter> _resolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="EngineIOMessageAdapterFactory"/> class.
    /// </summary>
    public EngineIOMessageAdapterFactory(Func<EngineIOVersion, IEngineIOMessageAdapter> resolver)
    {
        _resolver = resolver;
    }

    /// <inheritdoc />
    public IEngineIOMessageAdapter Create(EngineIOVersion engineIOVersion)
    {
        return _resolver(engineIOVersion);
    }
}
