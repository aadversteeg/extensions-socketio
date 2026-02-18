using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ave.Extensions.SocketIO.Messages;

namespace Ave.Extensions.SocketIO.Client;

/// <summary>
/// Defines the public API for a Socket.IO client.
/// </summary>
public interface ISocketIOClient : IDisposable
{
    /// <summary>
    /// Gets the client options.
    /// </summary>
    SocketIOClientOptions Options { get; }

    /// <summary>
    /// Gets the session identifier assigned by the server.
    /// </summary>
    string? Id { get; }

    /// <summary>
    /// Gets a value indicating whether the client is currently connected.
    /// </summary>
    bool Connected { get; }

    /// <summary>
    /// Connects to the Socket.IO server.
    /// </summary>
    Task ConnectAsync();

    /// <summary>
    /// Connects to the Socket.IO server with cancellation support.
    /// </summary>
    Task ConnectAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Emits an event with data.
    /// </summary>
    Task EmitAsync(string eventName, IEnumerable<object> data, CancellationToken cancellationToken);

    /// <summary>
    /// Emits an event with data.
    /// </summary>
    Task EmitAsync(string eventName, IEnumerable<object> data);

    /// <summary>
    /// Emits an event without data.
    /// </summary>
    Task EmitAsync(string eventName, CancellationToken cancellationToken);

    /// <summary>
    /// Emits an event without data.
    /// </summary>
    Task EmitAsync(string eventName);

    /// <summary>
    /// Emits an event with data and waits for an acknowledgement.
    /// </summary>
    Task EmitAsync(string eventName, IEnumerable<object> data, Func<IDataMessage, Task> ack, CancellationToken cancellationToken);

    /// <summary>
    /// Emits an event with data and waits for an acknowledgement.
    /// </summary>
    Task EmitAsync(string eventName, IEnumerable<object> data, Func<IDataMessage, Task> ack);

    /// <summary>
    /// Emits an event and waits for an acknowledgement.
    /// </summary>
    Task EmitAsync(string eventName, Func<IDataMessage, Task> ack, CancellationToken cancellationToken);

    /// <summary>
    /// Emits an event and waits for an acknowledgement.
    /// </summary>
    Task EmitAsync(string eventName, Func<IDataMessage, Task> ack);

    /// <summary>
    /// Disconnects from the Socket.IO server.
    /// </summary>
    Task DisconnectAsync();

    /// <summary>
    /// Disconnects from the Socket.IO server with cancellation support.
    /// </summary>
    Task DisconnectAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Registers a handler for the specified event.
    /// </summary>
    void On(string eventName, Func<IEventContext, Task> handler);

    /// <summary>
    /// Registers a handler that is invoked for any event.
    /// </summary>
    void OnAny(Func<string, IEventContext, Task> handler);

    /// <summary>
    /// Gets all registered OnAny handlers.
    /// </summary>
    IEnumerable<Func<string, IEventContext, Task>> ListenersAny { get; }

    /// <summary>
    /// Removes a previously registered OnAny handler.
    /// </summary>
    void OffAny(Func<string, IEventContext, Task> handler);

    /// <summary>
    /// Registers a handler that is invoked for any event, at the beginning of the handler list.
    /// </summary>
    void PrependAny(Func<string, IEventContext, Task> handler);

    /// <summary>
    /// Registers a handler that is invoked only once for the specified event.
    /// </summary>
    void Once(string eventName, Func<IEventContext, Task> handler);
}
