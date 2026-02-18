using System;

namespace Ave.Extensions.SocketIO.Client.Session;

/// <summary>
/// Exception thrown when a session connection attempt fails.
/// </summary>
public class ConnectionFailedException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionFailedException"/> class.
    /// </summary>
    public ConnectionFailedException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionFailedException"/> class.
    /// </summary>
    public ConnectionFailedException(Exception innerException) : this("Failed to connect to the server", innerException)
    {
    }
}
