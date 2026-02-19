using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Ave.Extensions.SocketIO.Messages;
using Ave.Extensions.SocketIO.Protocol;
using Ave.Extensions.SocketIO.Serialization;
using Ave.Extensions.SocketIO.Server.EngineIO;
using Ave.Extensions.SocketIO.Server.EngineIO.Transport;

namespace Ave.Extensions.SocketIO.Server.Middleware;

/// <summary>
/// ASP.NET Core middleware handling Socket.IO/Engine.IO HTTP requests.
/// </summary>
public class SocketIOMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SocketIOServerOptions _options;
    private readonly IEngineIOSessionStore _sessionStore;
    private readonly IPollingTransportHandler _pollingHandler;
    private readonly IWebSocketTransportHandler _webSocketHandler;
    private readonly ISerializer _serializer;
    private readonly IMessageRouter _messageRouter;
    private readonly MessageRouter _messageRouterImpl;
    private readonly SocketIOServer _server;
    private readonly ILogger<SocketIOMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SocketIOMiddleware"/> class.
    /// </summary>
    public SocketIOMiddleware(
        RequestDelegate next,
        SocketIOServerOptions options,
        IEngineIOSessionStore sessionStore,
        IPollingTransportHandler pollingHandler,
        IWebSocketTransportHandler webSocketHandler,
        ISerializer serializer,
        IMessageRouter messageRouter,
        ISocketIOServer server,
        ILogger<SocketIOMiddleware> logger)
    {
        _next = next;
        _options = options;
        _sessionStore = sessionStore;
        _pollingHandler = pollingHandler;
        _webSocketHandler = webSocketHandler;
        _serializer = serializer;
        _messageRouter = messageRouter;
        _messageRouterImpl = (MessageRouter)messageRouter;
        _server = (SocketIOServer)server;
        _logger = logger;
    }

    /// <summary>
    /// Processes an HTTP request.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (!path.StartsWith(_options.Path, StringComparison.OrdinalIgnoreCase))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        var query = context.Request.Query;
        var eioStr = query["EIO"].FirstOrDefault();
        var transport = query["transport"].FirstOrDefault();
        var sid = query["sid"].FirstOrDefault();

        if (string.IsNullOrEmpty(eioStr) || string.IsNullOrEmpty(transport))
        {
            context.Response.StatusCode = 400;
            await WriteJsonError(context, 0, "Missing required parameters").ConfigureAwait(false);
            return;
        }

        if (!int.TryParse(eioStr, out var eioInt))
        {
            context.Response.StatusCode = 400;
            await WriteJsonError(context, 0, "Invalid EIO version").ConfigureAwait(false);
            return;
        }

        var eioVersion = (EngineIOVersion)eioInt;
        if (!_options.AllowedEIOVersions.Contains(eioVersion))
        {
            context.Response.StatusCode = 400;
            await WriteJsonError(context, 0, "Unsupported EIO version").ConfigureAwait(false);
            return;
        }

        if (string.IsNullOrEmpty(sid))
        {
            await HandleNewSession(context, eioVersion, transport).ConfigureAwait(false);
        }
        else
        {
            await HandleExistingSession(context, sid, transport).ConfigureAwait(false);
        }
    }

    private async Task HandleNewSession(HttpContext context, EngineIOVersion eioVersion, string transport)
    {
        if (transport == "polling")
        {
            await HandleNewPollingSession(context, eioVersion).ConfigureAwait(false);
        }
        else if (transport == "websocket" && context.WebSockets.IsWebSocketRequest)
        {
            await HandleNewWebSocketSession(context, eioVersion).ConfigureAwait(false);
        }
        else
        {
            context.Response.StatusCode = 400;
            await WriteJsonError(context, 0, "Invalid transport").ConfigureAwait(false);
        }
    }

    private async Task HandleNewPollingSession(HttpContext context, EngineIOVersion eioVersion)
    {
        var transportProtocol = TransportProtocol.Polling;
        var session = _sessionStore.Create(eioVersion, transportProtocol);
        var handshake = CreateHandshake(context);

        SetupSession(session, handshake, eioVersion);

        var openPacket = BuildOpenPacket(session);

        _logger.LogDebug("New polling session {Sid} (EIO {Version})", session.Sid, eioVersion);

        context.Response.StatusCode = 200;
        context.Response.ContentType = "text/plain; charset=UTF-8";
        await context.Response.WriteAsync(openPacket, context.RequestAborted).ConfigureAwait(false);
    }

    private async Task HandleNewWebSocketSession(HttpContext context, EngineIOVersion eioVersion)
    {
        var session = _sessionStore.Create(eioVersion, TransportProtocol.WebSocket);
        var handshake = CreateHandshake(context);

        SetupSession(session, handshake, eioVersion);

        var openPacket = BuildOpenPacket(session);
        var webSocket = await context.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);

        _logger.LogDebug("New WebSocket session {Sid} (EIO {Version})", session.Sid, eioVersion);

        // Send handshake directly over WebSocket (before HandleAsync sets up the send callback)
        var openBytes = System.Text.Encoding.UTF8.GetBytes(openPacket);
        await webSocket.SendAsync(
            new ArraySegment<byte>(openBytes),
            System.Net.WebSockets.WebSocketMessageType.Text,
            true,
            context.RequestAborted).ConfigureAwait(false);

        await _webSocketHandler.HandleAsync(webSocket, session, context.RequestAborted).ConfigureAwait(false);
    }

    private async Task HandleExistingSession(HttpContext context, string sid, string transport)
    {
        var session = _sessionStore.Get(sid);
        if (session == null)
        {
            context.Response.StatusCode = 400;
            await WriteJsonError(context, 1, "Session ID unknown").ConfigureAwait(false);
            return;
        }

        if (transport == "polling")
        {
            if (context.Request.Method == "GET")
            {
                await _pollingHandler.HandleGetAsync(context, session, context.RequestAborted).ConfigureAwait(false);
            }
            else if (context.Request.Method == "POST")
            {
                await _pollingHandler.HandlePostAsync(context, session, context.RequestAborted).ConfigureAwait(false);
            }
        }
        else if (transport == "websocket" && context.WebSockets.IsWebSocketRequest)
        {
            var webSocket = await context.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);
            await _webSocketHandler.HandleUpgradeAsync(webSocket, session, context.RequestAborted).ConfigureAwait(false);
        }
    }

    private void SetupSession(IEngineIOSession session, Handshake handshake, EngineIOVersion eioVersion)
    {
        // Set up message routing: when Engine.IO receives a Socket.IO message, route it
        session.OnMessage = async msg =>
        {
            if (msg.Type == ProtocolMessageType.Text && msg.Text != null)
            {
                await HandleEngineIOMessageAsync(session, handshake, msg.Text, eioVersion).ConfigureAwait(false);
            }
        };

        session.OnClose = () =>
        {
            _logger.LogDebug("Session {Sid} closed", session.Sid);
            _messageRouterImpl.HandleSessionCloseAsync(session.Sid).ConfigureAwait(false);
            _sessionStore.Remove(session.Sid);
        };
    }

    private async Task HandleEngineIOMessageAsync(IEngineIOSession session, Handshake handshake, string text, EngineIOVersion eioVersion)
    {
        // Engine.IO level messages
        if (text == "2") // Ping
        {
            if (eioVersion == EngineIOVersion.V3)
            {
                // V3: client sends ping, server responds pong
                await session.SendAsync("3", CancellationToken.None).ConfigureAwait(false);
            }
            return;
        }
        if (text == "3") // Pong
        {
            // V4: server sent ping, client responds pong — heartbeat is alive
            return;
        }
        if (text == "1") // Close
        {
            await session.CloseAsync().ConfigureAwait(false);
            return;
        }

        // Socket.IO CONNECT (40) — parse directly since client sends bare "40" or "40/namespace,"
        // optionally followed by auth JSON, which differs from server-sent "40{"sid":"..."}"
        if (text.StartsWith("40"))
        {
            var (connectMessage, auth) = ParseConnectPacket(text);
            if (auth.HasValue)
            {
                handshake.Auth = auth.Value;
            }
            await _messageRouter.RouteAsync(connectMessage, session, handshake).ConfigureAwait(false);
            return;
        }

        // Socket.IO DISCONNECT (41) — parse directly for same reason
        if (text.StartsWith("41"))
        {
            var disconnectMessage = ParseDisconnectPacket(text);
            await _messageRouter.RouteAsync(disconnectMessage, session, handshake).ConfigureAwait(false);
            return;
        }

        // Other Socket.IO messages (events 42, acks 43, binary 45/46, etc.)
        var message = _serializer.Deserialize(text);
        if (message != null)
        {
            await _messageRouter.RouteAsync(message, session, handshake).ConfigureAwait(false);
        }
    }

    private static (ConnectedMessage Message, JsonElement? Auth) ParseConnectPacket(string text)
    {
        // Format: "40" (default namespace) or "40/namespace," or "40{auth json}" or "40/namespace,{auth json}"
        var data = text.Substring(2); // strip "40"
        var message = new ConnectedMessage();
        string? authJson = null;

        if (string.IsNullOrEmpty(data))
        {
            message.Namespace = "/";
        }
        else if (data.StartsWith("/"))
        {
            var commaIndex = data.IndexOf(',');
            if (commaIndex >= 0)
            {
                message.Namespace = data.Substring(0, commaIndex);
                var remainder = data.Substring(commaIndex + 1);
                if (!string.IsNullOrEmpty(remainder))
                {
                    authJson = remainder;
                }
            }
            else
            {
                message.Namespace = data;
            }
        }
        else
        {
            message.Namespace = "/";
            authJson = data;
        }

        JsonElement? auth = null;
        if (authJson != null)
        {
            try
            {
                auth = JsonDocument.Parse(authJson).RootElement.Clone();
            }
            catch (JsonException)
            {
                // Malformed auth JSON — ignore
            }
        }

        return (message, auth);
    }

    private static DisconnectedMessage ParseDisconnectPacket(string text)
    {
        var data = text.Substring(2); // strip "41"
        var message = new DisconnectedMessage();

        if (!string.IsNullOrEmpty(data) && data.StartsWith("/"))
        {
            var commaIndex = data.IndexOf(',');
            message.Namespace = commaIndex >= 0 ? data.Substring(0, commaIndex) : data;
        }

        return message;
    }

    private string BuildOpenPacket(IEngineIOSession session)
    {
        var upgrades = _options.AllowUpgrades && _options.Transports.Contains(TransportProtocol.WebSocket)
            ? new[] { "websocket" }
            : Array.Empty<string>();

        var openData = new
        {
            sid = session.Sid,
            upgrades,
            pingInterval = _options.PingInterval,
            pingTimeout = _options.PingTimeout,
            maxPayload = _options.MaxPayload,
        };

        return "0" + JsonSerializer.Serialize(openData, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });
    }

    private static Handshake CreateHandshake(HttpContext context)
    {
        var headers = new Dictionary<string, string>();
        foreach (var header in context.Request.Headers)
        {
            headers[header.Key.ToLowerInvariant()] = header.Value.ToString();
        }

        var queryDict = new Dictionary<string, string>();
        foreach (var q in context.Request.Query)
        {
            queryDict[q.Key] = q.Value.ToString();
        }

        return new Handshake
        {
            Headers = headers,
            Query = queryDict,
            Address = context.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
            Time = DateTime.UtcNow,
        };
    }

    private static async Task WriteJsonError(HttpContext context, int code, string message)
    {
        context.Response.ContentType = "application/json";
        var error = JsonSerializer.Serialize(new { code, message });
        await context.Response.WriteAsync(error).ConfigureAwait(false);
    }
}
