using System;

namespace Ave.Extensions.SocketIO.Client;

/// <summary>
/// Exception thrown when a Socket.IO connection attempt fails.
/// </summary>
public class ConnectionException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionException"/> class.
    /// </summary>
    public ConnectionException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionException"/> class.
    /// </summary>
    public ConnectionException(string message, Exception innerException) : base(message, innerException) { }
}
