using System;
using System.Collections.Generic;

namespace Ave.Extensions.SocketIO.Server;

/// <summary>
/// Default implementation of client handshake metadata.
/// </summary>
public class Handshake : IHandshake
{
    /// <inheritdoc />
    public IReadOnlyDictionary<string, string>? Headers { get; set; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string>? Query { get; set; }

    /// <inheritdoc />
    public object? Auth { get; set; }

    /// <inheritdoc />
    public string Address { get; set; } = string.Empty;

    /// <inheritdoc />
    public DateTime Time { get; set; } = DateTime.UtcNow;
}
