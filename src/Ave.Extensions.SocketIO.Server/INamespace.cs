using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ave.Extensions.SocketIO.Server;

/// <summary>
/// Represents a Socket.IO namespace (communication scope).
/// </summary>
public interface INamespace
{
    /// <summary>
    /// Gets the namespace path (e.g., "/", "/chat").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Registers a connection handler.
    /// </summary>
    void OnConnection(Func<IServerSocket, Task> handler);

    /// <summary>
    /// Registers middleware that runs before a connection is established.
    /// </summary>
    void Use(Func<IServerSocket, Func<Task>, Task> middleware);

    /// <summary>
    /// Emits an event to all connected sockets in this namespace.
    /// </summary>
    Task EmitAsync(string eventName, IEnumerable<object> data);

    /// <summary>
    /// Emits an event without data to all connected sockets in this namespace.
    /// </summary>
    Task EmitAsync(string eventName);

    /// <summary>
    /// Gets a broadcast operator targeting the specified room.
    /// </summary>
    IBroadcastOperator To(string room);

    /// <summary>
    /// Gets a broadcast operator targeting the specified rooms.
    /// </summary>
    IBroadcastOperator To(IEnumerable<string> rooms);

    /// <summary>
    /// Gets a broadcast operator excluding the specified room.
    /// </summary>
    IBroadcastOperator Except(string room);

    /// <summary>
    /// Gets a broadcast operator excluding the specified rooms.
    /// </summary>
    IBroadcastOperator Except(IEnumerable<string> rooms);

    /// <summary>
    /// Gets all connected socket IDs in this namespace.
    /// </summary>
    IReadOnlyCollection<string> SocketIds { get; }

    /// <summary>
    /// Gets a socket by its identifier, or null if not found.
    /// </summary>
    IServerSocket? GetSocket(string id);
}
