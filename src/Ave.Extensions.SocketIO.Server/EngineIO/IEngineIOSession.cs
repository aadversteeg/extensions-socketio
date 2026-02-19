using System;
using System.Threading;
using System.Threading.Tasks;
using Ave.Extensions.SocketIO.Protocol;

namespace Ave.Extensions.SocketIO.Server.EngineIO;

/// <summary>
/// Represents an Engine.IO transport session for a single client.
/// </summary>
public interface IEngineIOSession : IDisposable
{
    /// <summary>
    /// Gets the session identifier.
    /// </summary>
    string Sid { get; }

    /// <summary>
    /// Gets the Engine.IO protocol version for this session.
    /// </summary>
    EngineIOVersion Version { get; }

    /// <summary>
    /// Gets the current transport protocol.
    /// </summary>
    TransportProtocol CurrentTransport { get; }

    /// <summary>
    /// Gets whether the session is currently open.
    /// </summary>
    bool IsOpen { get; }

    /// <summary>
    /// Gets whether the session is in the process of upgrading transport.
    /// </summary>
    bool IsUpgrading { get; }

    /// <summary>
    /// Sends a protocol message to the client.
    /// </summary>
    Task SendAsync(ProtocolMessage message, CancellationToken cancellationToken);

    /// <summary>
    /// Sends a text message to the client.
    /// </summary>
    Task SendAsync(string text, CancellationToken cancellationToken);

    /// <summary>
    /// Sends binary data to the client.
    /// </summary>
    Task SendAsync(byte[] bytes, CancellationToken cancellationToken);

    /// <summary>
    /// Processes an incoming message from the client transport.
    /// </summary>
    Task ReceiveAsync(ProtocolMessage message);

    /// <summary>
    /// Waits for and dequeues pending messages for polling transport.
    /// </summary>
    Task<ProtocolMessage[]> DrainAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Upgrades the transport to WebSocket.
    /// </summary>
    void UpgradeTransport();

    /// <summary>
    /// Sets the callback invoked when a Socket.IO-level message is received.
    /// </summary>
    Func<ProtocolMessage, Task>? OnMessage { get; set; }

    /// <summary>
    /// Sets the callback invoked when the session is closed.
    /// </summary>
    Action? OnClose { get; set; }

    /// <summary>
    /// Closes the session.
    /// </summary>
    Task CloseAsync();
}
