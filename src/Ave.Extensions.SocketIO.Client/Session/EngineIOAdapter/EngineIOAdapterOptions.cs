using System;

namespace Ave.Extensions.SocketIO.Client.Session.EngineIOAdapter;

/// <summary>
/// Configuration options for Engine.IO adapters.
/// </summary>
public class EngineIOAdapterOptions
{
    /// <summary>
    /// Gets or sets the connection timeout.
    /// </summary>
    public TimeSpan Timeout { get; set; }

    /// <summary>
    /// Gets or sets the Socket.IO namespace.
    /// </summary>
    public string? Namespace { get; set; }

    /// <summary>
    /// Gets or sets the authentication payload.
    /// </summary>
    public object? Auth { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to automatically upgrade transport.
    /// </summary>
    public bool AutoUpgrade { get; set; }
}
