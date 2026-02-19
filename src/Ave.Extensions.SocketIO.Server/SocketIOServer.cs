using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Ave.Extensions.SocketIO.Serialization;
using Ave.Extensions.SocketIO.Server.EngineIO;
using Ave.Extensions.SocketIO.Server.Rooms;

namespace Ave.Extensions.SocketIO.Server;

/// <summary>
/// Socket.IO server implementation managing namespaces and connections.
/// </summary>
public class SocketIOServer : ISocketIOServer
{
    private readonly ConcurrentDictionary<string, Namespace> _namespaces =
        new ConcurrentDictionary<string, Namespace>();

    private readonly ISerializer _serializer;
    private readonly ISessionIdGenerator _idGenerator;
    private readonly ILogger<SocketIOServer> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SocketIOServer"/> class.
    /// </summary>
    public SocketIOServer(
        SocketIOServerOptions options,
        ISerializer serializer,
        ISessionIdGenerator idGenerator,
        ILogger<SocketIOServer> logger)
    {
        Options = options;
        _serializer = serializer;
        _idGenerator = idGenerator;
        _logger = logger;

        // Always create the default namespace
        GetOrCreateNamespace("/");
    }

    /// <inheritdoc />
    public SocketIOServerOptions Options { get; }

    /// <inheritdoc />
    public INamespace Of(string nsp)
    {
        return GetOrCreateNamespace(nsp);
    }

    /// <inheritdoc />
    public INamespace Default => GetOrCreateNamespace("/");

    /// <inheritdoc />
    public async Task EmitAsync(string eventName, IEnumerable<object> data)
    {
        await Default.EmitAsync(eventName, data).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task EmitAsync(string eventName)
    {
        await Default.EmitAsync(eventName).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void OnConnection(Func<IServerSocket, Task> handler)
    {
        Default.OnConnection(handler);
    }

    /// <inheritdoc />
    public IBroadcastOperator To(string room)
    {
        return Default.To(room);
    }

    /// <summary>
    /// Gets or creates a namespace. Used internally by the message router.
    /// </summary>
    internal Namespace GetOrCreateNamespace(string name)
    {
        return _namespaces.GetOrAdd(name, n =>
        {
            _logger.LogDebug("Creating namespace '{Namespace}'", n);
            return new Namespace(n, _serializer, new RoomManager(), _idGenerator, _logger);
        });
    }

    /// <summary>
    /// Gets a namespace by name, or null if not found.
    /// </summary>
    internal Namespace? GetNamespace(string name)
    {
        _namespaces.TryGetValue(name, out var ns);
        return ns;
    }
}
