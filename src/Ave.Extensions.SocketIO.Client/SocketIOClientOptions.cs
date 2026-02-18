using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Ave.Extensions.SocketIO.Client;

/// <summary>
/// Configuration options for the Socket.IO client.
/// </summary>
public class SocketIOClientOptions
{
    /// <summary>
    /// Gets or sets the Engine.IO protocol version. Default is V4.
    /// </summary>
    public EngineIOVersion EIO { get; set; } = EngineIOVersion.V4;

    /// <summary>
    /// Gets or sets the connection timeout. Default is 30 seconds.
    /// </summary>
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets a value indicating whether automatic reconnection is enabled. Default is true.
    /// </summary>
    public bool Reconnection { get; set; } = true;

    /// <summary>
    /// Gets or sets the transport protocol. Default is Polling.
    /// </summary>
    public TransportProtocol Transport { get; set; }

    private int _reconnectionAttempts = 10;

    /// <summary>
    /// Gets or sets the maximum number of reconnection attempts. Minimum value is 1. Default is 10.
    /// </summary>
    public int ReconnectionAttempts
    {
        get => _reconnectionAttempts;
        set
        {
            if (value < 1)
            {
                throw new ArgumentException("The minimum allowable number of attempts is 1");
            }
            _reconnectionAttempts = value;
        }
    }

    /// <summary>
    /// Gets or sets the maximum delay in milliseconds between reconnection attempts. Default is 5000.
    /// </summary>
    public int ReconnectionDelayMax { get; set; } = 5000;

    private string? _path;

    /// <summary>
    /// Gets or sets the Socket.IO server path.
    /// </summary>
    public string? Path
    {
        get => _path;
        set => _path = $"/{value!.Trim('/')}/";
    }

    /// <summary>
    /// Gets or sets query string parameters to include in the connection URL.
    /// </summary>
    public NameValueCollection? Query { get; set; }

    /// <summary>
    /// Gets or sets extra HTTP headers to include in requests.
    /// </summary>
    public IReadOnlyDictionary<string, string>? ExtraHeaders { get; set; }

    /// <summary>
    /// Gets or sets the authentication credentials to send during connection.
    /// </summary>
    public object? Auth { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to automatically upgrade from polling to WebSocket. Default is true.
    /// </summary>
    public bool AutoUpgrade { get; set; } = true;
}
