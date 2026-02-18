using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Ave.Extensions.SocketIO.Client.Protocol.WebSocket;
using Ave.Extensions.SocketIO.Client.Session.EngineIOAdapter;
using Ave.Extensions.SocketIO.Client.Session.WebSocket.EngineIOAdapter;
using Ave.Extensions.SocketIO.Protocol;
using Ave.Extensions.SocketIO.Serialization;

namespace Ave.Extensions.SocketIO.Client.Session.WebSocket;

/// <summary>
/// WebSocket transport session implementation.
/// </summary>
public class WebSocketSession : SessionBase<IWebSocketEngineIOAdapter>
{
    private readonly ISerializer _serializer;
    private readonly IWebSocketAdapter _wsAdapter;
    private readonly ILogger<WebSocketSession> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebSocketSession"/> class.
    /// </summary>
    public WebSocketSession(
        ILogger<WebSocketSession> logger,
        IEngineIOAdapterFactory engineIOAdapterFactory,
        IWebSocketAdapter wsAdapter,
        ISerializer serializer,
        IEngineIOMessageAdapterFactory engineIOMessageAdapterFactory)
        : base(logger, engineIOAdapterFactory, wsAdapter, serializer, engineIOMessageAdapterFactory)
    {
        _logger = logger;
        _serializer = serializer;
        _wsAdapter = wsAdapter;
    }

    /// <inheritdoc />
    protected override TransportProtocol Protocol => TransportProtocol.WebSocket;

    /// <inheritdoc />
    public override async Task OnNextAsync(ProtocolMessage message)
    {
        if (message.Type == ProtocolMessageType.Bytes)
        {
            message.Bytes = EngineIOAdapter.ReadProtocolFrame(message.Bytes!);
        }
        await HandleMessageAsync(message).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override async Task SendAsync(object[] data, CancellationToken cancellationToken)
    {
        var messages = _serializer.Serialize(data);
        await SendProtocolMessagesAsync(messages, cancellationToken).ConfigureAwait(false);
    }

    private async Task SendProtocolMessagesAsync(IEnumerable<ProtocolMessage> messages, CancellationToken cancellationToken)
    {
        foreach (var message in messages)
        {
            if (message.Type == ProtocolMessageType.Text)
            {
                _logger.LogDebug("[WebSocket] {message}", message.Text);
            }
            else
            {
                message.Bytes = EngineIOAdapter.WriteProtocolFrame(message.Bytes!);
                _logger.LogDebug("[WebSocket] binary {length}", message.Bytes.Length);
            }

            await _wsAdapter.SendAsync(message, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public override async Task SendAsync(object[] data, int packetId, CancellationToken cancellationToken)
    {
        var messages = _serializer.Serialize(data, packetId);
        await SendProtocolMessagesAsync(messages, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override async Task SendAckDataAsync(object[] data, int packetId, CancellationToken cancellationToken)
    {
        var messages = _serializer.SerializeAckData(data, packetId);
        await SendProtocolMessagesAsync(messages, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override async Task ConnectCoreAsync(Uri uri, CancellationToken cancellationToken)
    {
        await _wsAdapter.ConnectAsync(uri, cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrEmpty(Options.Sid))
        {
            var message = new ProtocolMessage { Text = "5" };
            _logger.LogDebug("[WebSocket] {message}", message.Text);
            await _wsAdapter.SendAsync(message, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    protected override string GetServerUriSchema()
    {
        var schema = Options.ServerUri.Scheme.ToLowerInvariant();
        return schema switch
        {
            "http" or "ws" => "ws",
            "https" or "wss" => "wss",
            _ => throw new ArgumentException("Only supports 'http, https, ws, wss' protocol")
        };
    }

    /// <inheritdoc />
    protected override NameValueCollection GetProtocolQueries()
    {
        var query = new NameValueCollection
        {
            ["transport"] = "websocket"
        };
        if (!string.IsNullOrEmpty(Options.Sid))
        {
            query["sid"] = Options.Sid;
        }
        return query;
    }

    /// <inheritdoc />
    public override async Task DisconnectAsync(CancellationToken cancellationToken)
    {
        var content = string.IsNullOrEmpty(Options.Namespace) ? "41" : $"41{Options.Namespace},";
        var message = new ProtocolMessage { Text = content };
        await _wsAdapter.SendAsync(message, cancellationToken).ConfigureAwait(false);
    }
}
