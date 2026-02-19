using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ave.Extensions.SocketIO.Server;

/// <summary>
/// Provides context for handling Socket.IO events on the server side.
/// </summary>
public interface IServerEventContext
{
    /// <summary>
    /// Gets a typed value from the event data at the specified index.
    /// </summary>
    T? GetValue<T>(int index);

    /// <summary>
    /// Gets a value of the specified type from the event data at the specified index.
    /// </summary>
    object? GetValue(Type type, int index);

    /// <summary>
    /// Sends acknowledgement data back to the client.
    /// </summary>
    Task SendAckDataAsync(IEnumerable<object> data);

    /// <summary>
    /// Sends acknowledgement data back to the client with cancellation support.
    /// </summary>
    Task SendAckDataAsync(IEnumerable<object> data, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the raw text of the event data.
    /// </summary>
    string RawText { get; }
}
