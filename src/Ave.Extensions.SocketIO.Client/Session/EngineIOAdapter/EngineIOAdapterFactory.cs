using System;
using Microsoft.Extensions.DependencyInjection;

namespace Ave.Extensions.SocketIO.Client.Session.EngineIOAdapter;

/// <summary>
/// Factory for creating Engine.IO adapters using keyed DI services.
/// </summary>
public class EngineIOAdapterFactory : IEngineIOAdapterFactory
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="EngineIOAdapterFactory"/> class.
    /// </summary>
    public EngineIOAdapterFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public T Create<T>(EngineIOCompatibility compatibility) where T : IEngineIOAdapter
    {
        return _serviceProvider.GetRequiredKeyedService<T>(compatibility);
    }
}
