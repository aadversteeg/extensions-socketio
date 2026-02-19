using System.Collections.Concurrent;

namespace Ave.Extensions.SocketIO.Server.EngineIO;

/// <summary>
/// Thread-safe in-memory store for Engine.IO sessions.
/// </summary>
public class EngineIOSessionStore : IEngineIOSessionStore
{
    private readonly ConcurrentDictionary<string, IEngineIOSession> _sessions =
        new ConcurrentDictionary<string, IEngineIOSession>();

    private readonly ISessionIdGenerator _idGenerator;

    /// <summary>
    /// Initializes a new instance of the <see cref="EngineIOSessionStore"/> class.
    /// </summary>
    public EngineIOSessionStore(ISessionIdGenerator idGenerator)
    {
        _idGenerator = idGenerator;
    }

    /// <inheritdoc />
    public IEngineIOSession Create(EngineIOVersion version, TransportProtocol transport)
    {
        var sid = _idGenerator.Generate();
        var session = new EngineIOSession(sid, version, transport);
        _sessions.TryAdd(sid, session);
        return session;
    }

    /// <inheritdoc />
    public IEngineIOSession? Get(string sid)
    {
        _sessions.TryGetValue(sid, out var session);
        return session;
    }

    /// <inheritdoc />
    public bool Remove(string sid)
    {
        if (_sessions.TryRemove(sid, out var session))
        {
            session.Dispose();
            return true;
        }
        return false;
    }

    /// <inheritdoc />
    public int Count => _sessions.Count;
}
