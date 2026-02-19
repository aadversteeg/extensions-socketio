using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Ave.Extensions.SocketIO.Server.EngineIO.Transport;

/// <summary>
/// Handles WebSocket transport for Engine.IO sessions.
/// </summary>
public interface IWebSocketTransportHandler
{
    /// <summary>
    /// Handles the WebSocket connection lifecycle for a new session.
    /// </summary>
    Task HandleAsync(WebSocket webSocket, IEngineIOSession session, CancellationToken cancellationToken);

    /// <summary>
    /// Handles the WebSocket transport upgrade for an existing polling session.
    /// </summary>
    Task HandleUpgradeAsync(WebSocket webSocket, IEngineIOSession session, CancellationToken cancellationToken);
}
