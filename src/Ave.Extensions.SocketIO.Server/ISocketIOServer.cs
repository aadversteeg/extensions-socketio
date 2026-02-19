using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ave.Extensions.SocketIO.Server;

/// <summary>
/// Top-level Socket.IO server managing namespaces and connections.
/// </summary>
public interface ISocketIOServer
{
    /// <summary>
    /// Gets the server options.
    /// </summary>
    SocketIOServerOptions Options { get; }

    /// <summary>
    /// Gets or creates a namespace by path.
    /// </summary>
    INamespace Of(string nsp);

    /// <summary>
    /// Gets the default namespace ("/").
    /// </summary>
    INamespace Default { get; }

    /// <summary>
    /// Emits an event to all connected sockets in the default namespace.
    /// </summary>
    Task EmitAsync(string eventName, IEnumerable<object> data);

    /// <summary>
    /// Emits an event without data to all connected sockets in the default namespace.
    /// </summary>
    Task EmitAsync(string eventName);

    /// <summary>
    /// Registers a connection handler on the default namespace.
    /// </summary>
    void OnConnection(Func<IServerSocket, Task> handler);

    /// <summary>
    /// Gets a broadcast operator targeting the specified room in the default namespace.
    /// </summary>
    IBroadcastOperator To(string room);
}
