using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Ave.Extensions.SocketIO.Client.Protocol.WebSocket;

/// <summary>
/// System.Net.WebSockets.ClientWebSocket-based implementation of <see cref="IWebSocketClient"/>.
/// </summary>
public class SystemClientWebSocket : IWebSocketClient
{
    private readonly ClientWebSocket _ws;

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemClientWebSocket"/> class.
    /// </summary>
    public SystemClientWebSocket(ILogger<SystemClientWebSocket> logger, WebSocketOptions options)
    {
        _ws = new ClientWebSocket();
        if (options.Proxy != null)
        {
            _ws.Options.Proxy = options.Proxy;
            logger.LogInformation("WebSocket proxy is enabled");
        }

        if (options.RemoteCertificateValidationCallback != null)
        {
#if NET6_0_OR_GREATER
            _ws.Options.RemoteCertificateValidationCallback = options.RemoteCertificateValidationCallback;
#endif
        }
    }

    /// <inheritdoc />
    public WebSocketState State => _ws.State;

    /// <inheritdoc />
    public Task ConnectAsync(Uri uri, CancellationToken cancellationToken) =>
        _ws.ConnectAsync(uri, cancellationToken);

    /// <inheritdoc />
    public Task SendAsync(ArraySegment<byte> buffer, System.Net.WebSockets.WebSocketMessageType messageType,
        bool endOfMessage,
        CancellationToken cancellationToken) =>
        _ws.SendAsync(buffer, messageType, endOfMessage, cancellationToken);

    /// <inheritdoc />
    public Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken) =>
        _ws.ReceiveAsync(buffer, cancellationToken);

    /// <inheritdoc />
    public Task CloseAsync(WebSocketCloseStatus closeStatus, string desc, CancellationToken cancellationToken) =>
        _ws.CloseAsync(closeStatus, desc, cancellationToken);

    /// <inheritdoc />
    public void SetDefaultHeader(string name, string value) => _ws.Options.SetRequestHeader(name, value);

    /// <inheritdoc />
    public void Dispose() => _ws.Dispose();
}
