using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Ave.Extensions.SocketIO.Messages;
using Ave.Extensions.SocketIO.Protocol;
using Ave.Extensions.SocketIO.Serialization;
using Ave.Extensions.SocketIO.Server.EngineIO;
using Ave.Extensions.SocketIO.Server.Rooms;

namespace Ave.Extensions.SocketIO.Server;

/// <summary>
/// Represents a connected client socket on the server side.
/// </summary>
public class ServerSocket : IServerSocket
{
    private readonly IEngineIOSession _engineSession;
    private readonly ISerializer _serializer;
    private readonly IRoomManager _roomManager;
    private readonly Namespace _namespace;
    private readonly ILogger _logger;
    private readonly object _ackLock = new object();

    private readonly Dictionary<string, Func<IServerEventContext, Task>> _eventHandlers =
        new Dictionary<string, Func<IServerEventContext, Task>>();
    private readonly HashSet<string> _onceEvents = new HashSet<string>();
    private readonly List<Func<string, IServerEventContext, Task>> _onAnyHandlers =
        new List<Func<string, IServerEventContext, Task>>();
    private readonly List<Func<string, Task>> _disconnectHandlers = new List<Func<string, Task>>();
    private readonly Dictionary<int, Func<IDataMessage, Task>> _ackHandlers =
        new Dictionary<int, Func<IDataMessage, Task>>();

    private int _packetId;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServerSocket"/> class.
    /// </summary>
    public ServerSocket(
        string id,
        string nsp,
        IEngineIOSession engineSession,
        IHandshake handshake,
        ISerializer serializer,
        IRoomManager roomManager,
        Namespace ns,
        ILogger logger)
    {
        Id = id;
        Namespace = nsp;
        _engineSession = engineSession;
        Handshake = handshake;
        _serializer = serializer;
        _roomManager = roomManager;
        _namespace = ns;
        _logger = logger;
        Connected = true;
        Data = new Dictionary<string, object?>();

        // Auto-join own room
        _roomManager.Join(id, id);
    }

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public string Namespace { get; }

    /// <inheritdoc />
    public IHandshake Handshake { get; }

    /// <inheritdoc />
    public IReadOnlyCollection<string> Rooms => _roomManager.GetRooms(Id);

    /// <inheritdoc />
    public IDictionary<string, object?> Data { get; }

    /// <inheritdoc />
    public bool Connected { get; private set; }

    /// <inheritdoc />
    public void On(string eventName, Func<IServerEventContext, Task> handler)
    {
        _eventHandlers[eventName] = handler;
        _onceEvents.Remove(eventName);
    }

    /// <inheritdoc />
    public void Once(string eventName, Func<IServerEventContext, Task> handler)
    {
        On(eventName, handler);
        _onceEvents.Add(eventName);
    }

    /// <inheritdoc />
    public void OnAny(Func<string, IServerEventContext, Task> handler)
    {
        _onAnyHandlers.Add(handler);
    }

    /// <inheritdoc />
    public void OffAny(Func<string, IServerEventContext, Task> handler)
    {
        _onAnyHandlers.Remove(handler);
    }

    /// <inheritdoc />
    public void OnDisconnect(Func<string, Task> handler)
    {
        _disconnectHandlers.Add(handler);
    }

    /// <inheritdoc />
    public async Task EmitAsync(string eventName, IEnumerable<object> data)
    {
        var payload = new[] { eventName }.Concat(data).ToArray();
        var messages = _serializer.Serialize(payload);
        foreach (var msg in messages)
        {
            await _engineSession.SendAsync(msg, CancellationToken.None).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task EmitAsync(string eventName)
    {
        await EmitAsync(eventName, Enumerable.Empty<object>()).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task EmitAsync(string eventName, IEnumerable<object> data, Func<IDataMessage, Task> ack)
    {
        int packetId;
        lock (_ackLock)
        {
            _packetId++;
            packetId = _packetId;
            _ackHandlers.Add(packetId, ack);
        }

        var payload = new[] { eventName }.Concat(data).ToArray();
        var messages = _serializer.Serialize(payload, packetId);
        foreach (var msg in messages)
        {
            await _engineSession.SendAsync(msg, CancellationToken.None).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public Task JoinAsync(string room)
    {
        _roomManager.Join(Id, room);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task JoinAsync(IEnumerable<string> rooms)
    {
        _roomManager.Join(Id, rooms);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task LeaveAsync(string room)
    {
        _roomManager.Leave(Id, room);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public IBroadcastOperator To(string room)
    {
        return new BroadcastOperator(_namespace, _roomManager)
            .WithExclude(Id)
            .To(room);
    }

    /// <inheritdoc />
    public IBroadcastOperator To(IEnumerable<string> rooms)
    {
        return new BroadcastOperator(_namespace, _roomManager)
            .WithExclude(Id)
            .To(rooms);
    }

    /// <inheritdoc />
    public IBroadcastOperator Except(string room)
    {
        return new BroadcastOperator(_namespace, _roomManager)
            .WithExclude(Id)
            .Except(room);
    }

    /// <inheritdoc />
    public IBroadcastOperator Broadcast =>
        new BroadcastOperator(_namespace, _roomManager).WithExclude(Id);

    /// <inheritdoc />
    public async Task DisconnectAsync(bool close)
    {
        if (!Connected) return;

        // Send disconnect packet
        var disconnectText = string.IsNullOrEmpty(Namespace) || Namespace == "/"
            ? "41"
            : $"41{Namespace},";
        await _engineSession.SendAsync(disconnectText, CancellationToken.None).ConfigureAwait(false);

        await HandleDisconnectAsync(DisconnectReason.IOServerDisconnect).ConfigureAwait(false);

        if (close)
        {
            await _engineSession.CloseAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Handles an incoming event message by dispatching to registered handlers.
    /// </summary>
    internal async Task HandleEventAsync(IEventMessage eventMessage)
    {
        Func<int, object[], CancellationToken, Task>? sendAck = null;
        if (eventMessage.Id >= 0)
        {
            sendAck = SendAckDataAsync;
        }

        var ctx = new ServerEventContext(eventMessage, sendAck);

        // OnAny handlers
        foreach (var handler in _onAnyHandlers)
        {
            try
            {
                await handler(eventMessage.Event, ctx).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error in OnAny handler");
            }
        }

        // Named event handler
        if (_eventHandlers.TryGetValue(eventMessage.Event, out var eventHandler))
        {
            var isOnce = _onceEvents.Contains(eventMessage.Event);
            if (isOnce)
            {
                _eventHandlers.Remove(eventMessage.Event);
                _onceEvents.Remove(eventMessage.Event);
            }

            try
            {
                await eventHandler(ctx).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error in event handler for '{Event}'", eventMessage.Event);
            }
        }
    }

    /// <summary>
    /// Handles an incoming acknowledgement message.
    /// </summary>
    internal async Task HandleAckAsync(IDataMessage ackMessage)
    {
        Func<IDataMessage, Task>? handler;
        lock (_ackLock)
        {
            if (_ackHandlers.TryGetValue(ackMessage.Id, out handler))
            {
                _ackHandlers.Remove(ackMessage.Id);
            }
        }

        if (handler != null)
        {
            await handler(ackMessage).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Handles a disconnect event.
    /// </summary>
    internal async Task HandleDisconnectAsync(string reason)
    {
        if (!Connected) return;
        Connected = false;

        foreach (var handler in _disconnectHandlers)
        {
            try
            {
                await handler(reason).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error in disconnect handler");
            }
        }

        _roomManager.LeaveAll(Id);
    }

    private async Task SendAckDataAsync(int packetId, object[] data, CancellationToken cancellationToken)
    {
        var messages = _serializer.SerializeAckData(data, packetId);
        foreach (var msg in messages)
        {
            await _engineSession.SendAsync(msg, cancellationToken).ConfigureAwait(false);
        }
    }
}
