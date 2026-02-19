using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Ave.Extensions.SocketIO.Messages;
using Ave.Extensions.SocketIO.Serialization;
using Ave.Extensions.SocketIO.Server.EngineIO;
using Ave.Extensions.SocketIO.Server.Rooms;

namespace Ave.Extensions.SocketIO.Server;

/// <summary>
/// Socket.IO namespace managing connected sockets, middleware, and connection handlers.
/// </summary>
public class Namespace : INamespace
{
    private readonly ConcurrentDictionary<string, ServerSocket> _sockets =
        new ConcurrentDictionary<string, ServerSocket>();

    private readonly List<Func<IServerSocket, Task>> _connectionHandlers =
        new List<Func<IServerSocket, Task>>();

    private readonly List<Func<IServerSocket, Func<Task>, Task>> _middlewares =
        new List<Func<IServerSocket, Func<Task>, Task>>();

    private readonly ISerializer _serializer;
    private readonly IRoomManager _roomManager;
    private readonly ISessionIdGenerator _idGenerator;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="Namespace"/> class.
    /// </summary>
    public Namespace(
        string name,
        ISerializer serializer,
        IRoomManager roomManager,
        ISessionIdGenerator idGenerator,
        ILogger logger)
    {
        Name = name;
        _serializer = serializer;
        _roomManager = roomManager;
        _idGenerator = idGenerator;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public void OnConnection(Func<IServerSocket, Task> handler)
    {
        _connectionHandlers.Add(handler);
    }

    /// <inheritdoc />
    public void Use(Func<IServerSocket, Func<Task>, Task> middleware)
    {
        _middlewares.Add(middleware);
    }

    /// <inheritdoc />
    public async Task EmitAsync(string eventName, IEnumerable<object> data)
    {
        foreach (var socket in _sockets.Values)
        {
            await socket.EmitAsync(eventName, data).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task EmitAsync(string eventName)
    {
        await EmitAsync(eventName, Enumerable.Empty<object>()).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public IBroadcastOperator To(string room)
    {
        return new BroadcastOperator(this, _roomManager).To(room);
    }

    /// <inheritdoc />
    public IBroadcastOperator To(IEnumerable<string> rooms)
    {
        return new BroadcastOperator(this, _roomManager).To(rooms);
    }

    /// <inheritdoc />
    public IBroadcastOperator Except(string room)
    {
        return new BroadcastOperator(this, _roomManager).Except(room);
    }

    /// <inheritdoc />
    public IBroadcastOperator Except(IEnumerable<string> rooms)
    {
        return new BroadcastOperator(this, _roomManager).Except(rooms);
    }

    /// <inheritdoc />
    public IReadOnlyCollection<string> SocketIds => _sockets.Keys.ToList();

    /// <inheritdoc />
    public IServerSocket? GetSocket(string id)
    {
        _sockets.TryGetValue(id, out var socket);
        return socket;
    }

    /// <summary>
    /// Handles a Socket.IO CONNECT packet for this namespace.
    /// Returns the newly created socket ID, or null if middleware rejected the connection.
    /// </summary>
    internal async Task<string?> HandleConnectAsync(
        IEngineIOSession engineSession,
        IHandshake handshake,
        ConnectedMessage? connectMessage)
    {
        var socketId = _idGenerator.Generate();
        var socket = new ServerSocket(
            socketId, Name, engineSession, handshake,
            _serializer, _roomManager, this, _logger);

        // Run middleware chain
        var accepted = await RunMiddlewareAsync(socket).ConfigureAwait(false);
        if (!accepted)
        {
            var errorMessage = socket.Data.TryGetValue("error", out var errObj) && errObj is string errStr
                ? errStr
                : "middleware rejected";
            var escapedMessage = JsonSerializer.Serialize(new { message = errorMessage });
            var errorText = Name == "/"
                ? $"44{escapedMessage}"
                : $"44{Name},{escapedMessage}";
            await engineSession.SendAsync(errorText, CancellationToken.None).ConfigureAwait(false);
            return null;
        }

        _sockets.TryAdd(socketId, socket);

        // Send CONNECT response
        var sidJson = JsonSerializer.Serialize(new { sid = socketId });
        var response = Name == "/"
            ? $"40{sidJson}"
            : $"40{Name},{sidJson}";
        await engineSession.SendAsync(response, CancellationToken.None).ConfigureAwait(false);

        _logger.LogDebug("Socket {SocketId} connected to namespace '{Namespace}'", socketId, Name);

        // Invoke connection handlers
        foreach (var handler in _connectionHandlers)
        {
            try
            {
                await handler(socket).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error in connection handler for namespace '{Namespace}'", Name);
            }
        }

        return socketId;
    }

    /// <summary>
    /// Routes an event message to the appropriate socket.
    /// </summary>
    internal async Task HandleEventAsync(string socketId, IEventMessage eventMessage)
    {
        if (_sockets.TryGetValue(socketId, out var socket))
        {
            await socket.HandleEventAsync(eventMessage).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Routes an ack message to the appropriate socket.
    /// </summary>
    internal async Task HandleAckAsync(string socketId, IDataMessage ackMessage)
    {
        if (_sockets.TryGetValue(socketId, out var socket))
        {
            await socket.HandleAckAsync(ackMessage).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Handles a disconnect for a socket.
    /// </summary>
    internal async Task HandleDisconnectAsync(string socketId, string reason)
    {
        if (_sockets.TryRemove(socketId, out var socket))
        {
            await socket.HandleDisconnectAsync(reason).ConfigureAwait(false);
            _logger.LogDebug("Socket {SocketId} disconnected from namespace '{Namespace}': {Reason}",
                socketId, Name, reason);
        }
    }

    private async Task<bool> RunMiddlewareAsync(ServerSocket socket)
    {
        if (_middlewares.Count == 0) return true;

        var accepted = false;
        var index = 0;

        async Task Next()
        {
            index++;
            if (index < _middlewares.Count)
            {
                await _middlewares[index](socket, Next).ConfigureAwait(false);
            }
            else
            {
                accepted = true;
            }
        }

        try
        {
            await _middlewares[0](socket, Next).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Middleware error in namespace '{Namespace}'", Name);
            return false;
        }

        return accepted;
    }
}
