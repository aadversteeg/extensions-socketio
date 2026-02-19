using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ave.Extensions.SocketIO.Server;

/// <summary>
/// Fluent builder for broadcasting events to targeted rooms.
/// </summary>
public interface IBroadcastOperator
{
    /// <summary>
    /// Targets an additional room for broadcasting.
    /// </summary>
    IBroadcastOperator To(string room);

    /// <summary>
    /// Targets additional rooms for broadcasting.
    /// </summary>
    IBroadcastOperator To(IEnumerable<string> rooms);

    /// <summary>
    /// Excludes a room from the broadcast target.
    /// </summary>
    IBroadcastOperator Except(string room);

    /// <summary>
    /// Excludes rooms from the broadcast target.
    /// </summary>
    IBroadcastOperator Except(IEnumerable<string> rooms);

    /// <summary>
    /// Emits an event to all targeted sockets.
    /// </summary>
    Task EmitAsync(string eventName, IEnumerable<object> data);

    /// <summary>
    /// Emits an event without data to all targeted sockets.
    /// </summary>
    Task EmitAsync(string eventName);
}
