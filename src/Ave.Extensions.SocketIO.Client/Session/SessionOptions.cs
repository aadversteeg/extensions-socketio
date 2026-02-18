using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Ave.Extensions.SocketIO.Client.Session;

/// <summary>
/// Configuration options for a session.
/// </summary>
public class SessionOptions
{
    /// <summary>
    /// Gets or sets the server URI.
    /// </summary>
    public Uri ServerUri { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Socket.IO path.
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// Gets or sets the Socket.IO namespace.
    /// </summary>
    public string? Namespace { get; set; }

    /// <summary>
    /// Gets or sets query string parameters.
    /// </summary>
    public NameValueCollection? Query { get; set; }

    /// <summary>
    /// Gets or sets the connection timeout.
    /// </summary>
    public TimeSpan Timeout { get; set; }

    /// <summary>
    /// Gets or sets the Engine.IO protocol version.
    /// </summary>
    public EngineIOVersion EngineIO { get; set; }

    /// <summary>
    /// Gets or sets extra HTTP headers.
    /// </summary>
    public IReadOnlyDictionary<string, string>? ExtraHeaders { get; set; }

    /// <summary>
    /// Gets or sets the authentication payload.
    /// </summary>
    public object? Auth { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to automatically upgrade transport.
    /// </summary>
    public bool AutoUpgrade { get; set; }

    /// <summary>
    /// Gets or sets the session identifier for transport upgrades.
    /// </summary>
    public string? Sid { get; set; }
}
