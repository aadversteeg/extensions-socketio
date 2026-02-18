using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ave.Extensions.SocketIO.Client;

/// <summary>
/// Provides context for handling Socket.IO events, including access to event data and acknowledgement.
/// </summary>
public interface IEventContext
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
    /// Sends acknowledgement data back to the server.
    /// </summary>
    Task SendAckDataAsync(IEnumerable<object> data);

    /// <summary>
    /// Sends acknowledgement data back to the server with cancellation support.
    /// </summary>
    Task SendAckDataAsync(IEnumerable<object> data, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the raw text of the event data.
    /// </summary>
    string RawText { get; }
}
