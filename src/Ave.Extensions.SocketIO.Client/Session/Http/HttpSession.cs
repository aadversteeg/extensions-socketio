using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Ave.Extensions.SocketIO.Client.Protocol.Http;
using Ave.Extensions.SocketIO.Client.Session.EngineIOAdapter;
using Ave.Extensions.SocketIO.Client.Session.Http.EngineIOAdapter;
using Ave.Extensions.SocketIO.Messages;
using Ave.Extensions.SocketIO.Protocol;
using Ave.Extensions.SocketIO.Serialization;

namespace Ave.Extensions.SocketIO.Client.Session.Http;

/// <summary>
/// HTTP polling transport session implementation.
/// </summary>
public class HttpSession : SessionBase<IHttpEngineIOAdapter>
{
    private readonly ILogger<HttpSession> _logger;
    private readonly IHttpAdapter _httpAdapter;
    private readonly ISerializer _serializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpSession"/> class.
    /// </summary>
    public HttpSession(
        ILogger<HttpSession> logger,
        IEngineIOAdapterFactory engineIOAdapterFactory,
        IHttpAdapter httpAdapter,
        ISerializer serializer,
        IEngineIOMessageAdapterFactory engineIOMessageAdapterFactory)
        : base(logger, engineIOAdapterFactory, httpAdapter, serializer, engineIOMessageAdapterFactory)
    {
        _logger = logger;
        _httpAdapter = httpAdapter;
        _serializer = serializer;
    }

    /// <inheritdoc />
    protected override TransportProtocol Protocol => TransportProtocol.Polling;

    /// <inheritdoc />
    public override async Task OnNextAsync(ProtocolMessage message)
    {
        if (message.Type == ProtocolMessageType.Bytes)
        {
            var bytesMessages = EngineIOAdapter.ExtractMessagesFromBytes(message.Bytes!);
            await HandleMessagesAsync(bytesMessages).ConfigureAwait(false);
            return;
        }

        var messages = EngineIOAdapter.ExtractMessagesFromText(message.Text!);
        await HandleMessagesAsync(messages).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override void OnOpenedMessage(OpenedMessage message)
    {
        base.OnOpenedMessage(message);
        _httpAdapter.Uri = new Uri($"{_httpAdapter.Uri!.AbsoluteUri}&sid={message.Sid}");
    }

    private async Task HandleMessagesAsync(IEnumerable<ProtocolMessage> messages)
    {
        foreach (var message in messages)
        {
            await HandleMessageAsync(message).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public override async Task SendAsync(object[] data, CancellationToken cancellationToken)
    {
        var messages = _serializer.Serialize(data);
        await SendProtocolMessagesAsync(messages, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override async Task SendAsync(object[] data, int packetId, CancellationToken cancellationToken)
    {
        var messages = _serializer.Serialize(data, packetId);
        await SendProtocolMessagesAsync(messages, cancellationToken).ConfigureAwait(false);
    }

    private async Task SendProtocolMessagesAsync(IEnumerable<ProtocolMessage> messages, CancellationToken cancellationToken)
    {
        var bytes = new List<byte[]>();
        foreach (var message in messages)
        {
            if (message.Type == ProtocolMessageType.Text)
            {
                var request = EngineIOAdapter.ToHttpRequest(message.Text!);
                _logger.LogDebug("[Polling] {text}", request.BodyText);
                await _httpAdapter.SendAsync(request, cancellationToken).ConfigureAwait(false);
                continue;
            }
            _logger.LogDebug("[Polling] binary {length}", message.Bytes!.Length);
            bytes.Add(message.Bytes);
        }

        if (bytes.Count > 0)
        {
            var request = EngineIOAdapter.ToHttpRequest(bytes);
            await _httpAdapter.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
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
        _httpAdapter.Uri = uri;
        var req = new HttpRequest
        {
            Uri = uri,
            IsConnect = true
        };
        await _httpAdapter.SendAsync(req, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override string GetServerUriSchema()
    {
        var schema = Options.ServerUri.Scheme.ToLowerInvariant();
        return schema switch
        {
            "http" or "ws" => "http",
            "https" or "wss" => "https",
            _ => throw new ArgumentException("Only supports 'http, https, ws, wss' protocol")
        };
    }

    /// <inheritdoc />
    protected override NameValueCollection GetProtocolQueries()
    {
        return new NameValueCollection
        {
            ["transport"] = "polling"
        };
    }

    /// <inheritdoc />
    public override async Task DisconnectAsync(CancellationToken cancellationToken)
    {
        var content = string.IsNullOrEmpty(Options.Namespace) ? "41" : $"41{Options.Namespace},";
        var req = EngineIOAdapter.ToHttpRequest(content);
        await _httpAdapter.SendAsync(req, cancellationToken).ConfigureAwait(false);
    }
}
