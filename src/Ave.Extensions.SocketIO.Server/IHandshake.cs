using System;
using System.Collections.Generic;

namespace Ave.Extensions.SocketIO.Server;

/// <summary>
/// Provides client handshake metadata.
/// </summary>
public interface IHandshake
{
    /// <summary>
    /// Gets the request headers from the client.
    /// </summary>
    IReadOnlyDictionary<string, string>? Headers { get; }

    /// <summary>
    /// Gets the query parameters from the connection request.
    /// </summary>
    IReadOnlyDictionary<string, string>? Query { get; }

    /// <summary>
    /// Gets the authentication data sent by the client.
    /// </summary>
    object? Auth { get; }

    /// <summary>
    /// Gets the remote address of the client.
    /// </summary>
    string Address { get; }

    /// <summary>
    /// Gets the time of the connection.
    /// </summary>
    DateTime Time { get; }
}
