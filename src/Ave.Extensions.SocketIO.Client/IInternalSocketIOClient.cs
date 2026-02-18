using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ave.Extensions.SocketIO.Client.Observers;
using Ave.Extensions.SocketIO.Messages;

namespace Ave.Extensions.SocketIO.Client;

/// <summary>
/// Internal interface for session-to-client communication.
/// </summary>
public interface IInternalSocketIOClient : IMyObserver<IMessage>
{
    /// <summary>
    /// Gets the count of pending acknowledgement handlers.
    /// </summary>
    int AckHandlerCount { get; }

    /// <summary>
    /// Gets the current packet identifier.
    /// </summary>
    int PacketId { get; }

    /// <summary>
    /// Sends acknowledgement data for a specific packet.
    /// </summary>
    Task SendAckDataAsync(int packetId, IEnumerable<object> data);

    /// <summary>
    /// Sends acknowledgement data for a specific packet with cancellation support.
    /// </summary>
    Task SendAckDataAsync(int packetId, IEnumerable<object> data, CancellationToken cancellationToken);
}
