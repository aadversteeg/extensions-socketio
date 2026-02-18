using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Ave.Extensions.SocketIO.Client.Protocol.WebSocket;

/// <summary>
/// Abstracts a WebSocket client.
/// </summary>
public interface IWebSocketClient : IDisposable
{
    /// <summary>
    /// Gets the current state of the WebSocket connection.
    /// </summary>
    WebSocketState State { get; }

    /// <summary>
    /// Connects to a WebSocket server.
    /// </summary>
    Task ConnectAsync(Uri uri, CancellationToken cancellationToken);

    /// <summary>
    /// Sends data over the WebSocket connection.
    /// </summary>
    Task SendAsync(ArraySegment<byte> buffer, System.Net.WebSockets.WebSocketMessageType messageType, bool endOfMessage,
        CancellationToken cancellationToken);

    /// <summary>
    /// Receives data from the WebSocket connection.
    /// </summary>
    Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken);

    /// <summary>
    /// Closes the WebSocket connection.
    /// </summary>
    Task CloseAsync(WebSocketCloseStatus closeStatus, string desc, CancellationToken cancellationToken);

    /// <summary>
    /// Sets a default header for the WebSocket request.
    /// </summary>
    void SetDefaultHeader(string name, string value);
}
