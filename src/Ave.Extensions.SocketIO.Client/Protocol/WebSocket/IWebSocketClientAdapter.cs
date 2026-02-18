using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ave.Extensions.SocketIO.Client.Protocol.WebSocket;

/// <summary>
/// Abstracts a WebSocket client adapter with simplified send/receive operations.
/// </summary>
public interface IWebSocketClientAdapter
{
    /// <summary>
    /// Connects to a WebSocket server.
    /// </summary>
    Task ConnectAsync(Uri uri, CancellationToken cancellationToken);

    /// <summary>
    /// Sends data over the WebSocket connection.
    /// </summary>
    Task SendAsync(byte[] data, WebSocketMessageType messageType, CancellationToken cancellationToken);

    /// <summary>
    /// Receives a message from the WebSocket connection.
    /// </summary>
    Task<WebSocketMessage> ReceiveAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Sets a default header for the WebSocket request.
    /// </summary>
    void SetDefaultHeader(string name, string value);
}
