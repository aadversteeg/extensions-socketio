using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Ave.Extensions.SocketIO.Messages;
using Ave.Extensions.SocketIO.Server.EngineIO;

namespace Ave.Extensions.SocketIO.Server;

/// <summary>
/// Routes deserialized Socket.IO messages to the appropriate namespace and socket.
/// </summary>
public class MessageRouter : IMessageRouter
{
    private readonly SocketIOServer _server;
    private readonly ILogger<MessageRouter> _logger;

    // Maps Engine.IO sid → (namespace → Socket.IO socket id)
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, string>> _sessionSocketMap =
        new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>();

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageRouter"/> class.
    /// </summary>
    public MessageRouter(SocketIOServer server, ILogger<MessageRouter> logger)
    {
        _server = server;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task RouteAsync(IMessage message, IEngineIOSession session, IHandshake handshake)
    {
        switch (message.Type)
        {
            case MessageType.Connected:
                await HandleConnectAsync(session, handshake, (ConnectedMessage)message).ConfigureAwait(false);
                break;

            case MessageType.Disconnected:
                await HandleDisconnectAsync(session, message).ConfigureAwait(false);
                break;

            case MessageType.Event:
            case MessageType.Binary:
                await HandleEventAsync(session, (IEventMessage)message).ConfigureAwait(false);
                break;

            case MessageType.Ack:
            case MessageType.BinaryAck:
                await HandleAckAsync(session, (IDataMessage)message).ConfigureAwait(false);
                break;

            default:
                _logger.LogDebug("Unhandled Socket.IO message type: {Type}", message.Type);
                break;
        }
    }

    private async Task HandleConnectAsync(IEngineIOSession session, IHandshake handshake, ConnectedMessage connectMessage)
    {
        var nspName = connectMessage.Namespace ?? "/";
        var ns = _server.GetOrCreateNamespace(nspName);

        var socketId = await ns.HandleConnectAsync(session, handshake, connectMessage).ConfigureAwait(false);

        if (socketId != null)
        {
            var map = _sessionSocketMap.GetOrAdd(session.Sid,
                _ => new ConcurrentDictionary<string, string>());
            map[nspName] = socketId;
        }
    }

    private async Task HandleDisconnectAsync(IEngineIOSession session, IMessage message)
    {
        var nspName = "/";
        if (message is INamespaceMessage nsMsg && !string.IsNullOrEmpty(nsMsg.Namespace))
        {
            nspName = nsMsg.Namespace;
        }

        if (TryGetSocketId(session.Sid, nspName, out var socketId))
        {
            var ns = _server.GetNamespace(nspName);
            if (ns != null)
            {
                await ns.HandleDisconnectAsync(socketId!, DisconnectReason.IOClientDisconnect).ConfigureAwait(false);
            }
            RemoveSocketMapping(session.Sid, nspName);
        }
    }

    private async Task HandleEventAsync(IEngineIOSession session, IEventMessage eventMessage)
    {
        var nspName = eventMessage.Namespace ?? "/";
        if (TryGetSocketId(session.Sid, nspName, out var socketId))
        {
            var ns = _server.GetNamespace(nspName);
            if (ns != null)
            {
                await ns.HandleEventAsync(socketId!, eventMessage).ConfigureAwait(false);
            }
        }
    }

    private async Task HandleAckAsync(IEngineIOSession session, IDataMessage ackMessage)
    {
        var nspName = ackMessage.Namespace ?? "/";
        if (TryGetSocketId(session.Sid, nspName, out var socketId))
        {
            var ns = _server.GetNamespace(nspName);
            if (ns != null)
            {
                await ns.HandleAckAsync(socketId!, ackMessage).ConfigureAwait(false);
            }
        }
    }

    private bool TryGetSocketId(string sid, string nspName, out string? socketId)
    {
        socketId = null;
        if (_sessionSocketMap.TryGetValue(sid, out var map))
        {
            return map.TryGetValue(nspName, out socketId);
        }
        return false;
    }

    private void RemoveSocketMapping(string sid, string nspName)
    {
        if (_sessionSocketMap.TryGetValue(sid, out var map))
        {
            map.TryRemove(nspName, out _);
            if (map.IsEmpty)
            {
                _sessionSocketMap.TryRemove(sid, out _);
            }
        }
    }

    /// <summary>
    /// Handles disconnection of all sockets for an Engine.IO session.
    /// </summary>
    internal async Task HandleSessionCloseAsync(string sid)
    {
        if (_sessionSocketMap.TryRemove(sid, out var map))
        {
            foreach (var (nspName, socketId) in map)
            {
                var ns = _server.GetNamespace(nspName);
                if (ns != null)
                {
                    await ns.HandleDisconnectAsync(socketId, DisconnectReason.TransportClose).ConfigureAwait(false);
                }
            }
        }
    }
}
