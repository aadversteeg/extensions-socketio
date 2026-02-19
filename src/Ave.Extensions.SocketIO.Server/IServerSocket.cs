using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ave.Extensions.SocketIO.Messages;

namespace Ave.Extensions.SocketIO.Server;

/// <summary>
/// Represents a connected client socket on the server side.
/// </summary>
public interface IServerSocket
{
    /// <summary>
    /// Gets the unique socket identifier.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the namespace this socket belongs to.
    /// </summary>
    string Namespace { get; }

    /// <summary>
    /// Gets the client handshake metadata.
    /// </summary>
    IHandshake Handshake { get; }

    /// <summary>
    /// Gets the set of rooms this socket is in.
    /// </summary>
    IReadOnlyCollection<string> Rooms { get; }

    /// <summary>
    /// Gets the custom data dictionary for per-socket state.
    /// </summary>
    IDictionary<string, object?> Data { get; }

    /// <summary>
    /// Gets whether the socket is connected.
    /// </summary>
    bool Connected { get; }

    /// <summary>
    /// Registers a handler for the specified event.
    /// </summary>
    void On(string eventName, Func<IServerEventContext, Task> handler);

    /// <summary>
    /// Registers a handler that fires only once for the specified event.
    /// </summary>
    void Once(string eventName, Func<IServerEventContext, Task> handler);

    /// <summary>
    /// Registers a catch-all handler invoked for any event.
    /// </summary>
    void OnAny(Func<string, IServerEventContext, Task> handler);

    /// <summary>
    /// Removes a catch-all handler.
    /// </summary>
    void OffAny(Func<string, IServerEventContext, Task> handler);

    /// <summary>
    /// Registers a handler for the disconnect event.
    /// </summary>
    void OnDisconnect(Func<string, Task> handler);

    /// <summary>
    /// Emits an event with data to this socket.
    /// </summary>
    Task EmitAsync(string eventName, IEnumerable<object> data);

    /// <summary>
    /// Emits an event without data to this socket.
    /// </summary>
    Task EmitAsync(string eventName);

    /// <summary>
    /// Emits an event with data and waits for an acknowledgement from the client.
    /// </summary>
    Task EmitAsync(string eventName, IEnumerable<object> data, Func<IDataMessage, Task> ack);

    /// <summary>
    /// Joins a room.
    /// </summary>
    Task JoinAsync(string room);

    /// <summary>
    /// Joins multiple rooms.
    /// </summary>
    Task JoinAsync(IEnumerable<string> rooms);

    /// <summary>
    /// Leaves a room.
    /// </summary>
    Task LeaveAsync(string room);

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
    /// Gets a broadcast operator that emits to all sockets except this one.
    /// </summary>
    IBroadcastOperator Broadcast { get; }

    /// <summary>
    /// Disconnects this socket.
    /// </summary>
    Task DisconnectAsync(bool close = false);
}
