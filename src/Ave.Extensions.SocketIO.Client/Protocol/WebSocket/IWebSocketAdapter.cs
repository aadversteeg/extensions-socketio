using System;
using System.Threading;
using System.Threading.Tasks;
using Ave.Extensions.SocketIO.Protocol;

namespace Ave.Extensions.SocketIO.Client.Protocol.WebSocket;

/// <summary>
/// Defines a WebSocket transport adapter.
/// </summary>
public interface IWebSocketAdapter : IProtocolAdapter
{
    /// <summary>
    /// Connects to a WebSocket server.
    /// </summary>
    Task ConnectAsync(Uri uri, CancellationToken cancellationToken);

    /// <summary>
    /// Sends a protocol message over the WebSocket connection.
    /// </summary>
    Task SendAsync(ProtocolMessage message, CancellationToken cancellationToken);
}
