using System.Collections.Generic;

namespace Ave.Extensions.SocketIO.Server;

/// <summary>
/// Configuration options for the Socket.IO server.
/// </summary>
public class SocketIOServerOptions
{
    /// <summary>
    /// Gets or sets the path prefix for Socket.IO requests. Defaults to "/socket.io/".
    /// </summary>
    public string Path { get; set; } = "/socket.io/";

    /// <summary>
    /// Gets or sets the ping interval in milliseconds. Defaults to 25000.
    /// </summary>
    public int PingInterval { get; set; } = 25000;

    /// <summary>
    /// Gets or sets the ping timeout in milliseconds. Defaults to 20000.
    /// </summary>
    public int PingTimeout { get; set; } = 20000;

    /// <summary>
    /// Gets or sets the maximum payload size in bytes. Defaults to 1MB.
    /// </summary>
    public int MaxPayload { get; set; } = 1_000_000;

    /// <summary>
    /// Gets or sets the allowed transport protocols.
    /// </summary>
    public ISet<TransportProtocol> Transports { get; set; } = new HashSet<TransportProtocol>
    {
        TransportProtocol.Polling,
        TransportProtocol.WebSocket,
    };

    /// <summary>
    /// Gets or sets the allowed Engine.IO protocol versions.
    /// </summary>
    public ISet<EngineIOVersion> AllowedEIOVersions { get; set; } = new HashSet<EngineIOVersion>
    {
        EngineIOVersion.V3,
        EngineIOVersion.V4,
    };

    /// <summary>
    /// Gets or sets whether transport upgrades (polling to WebSocket) are allowed.
    /// </summary>
    public bool AllowUpgrades { get; set; } = true;
}
