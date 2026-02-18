using System;
using System.Threading;
using System.Threading.Tasks;
using Ave.Extensions.SocketIO.Client.Observers;
using Ave.Extensions.SocketIO.Messages;
using Ave.Extensions.SocketIO.Protocol;

namespace Ave.Extensions.SocketIO.Client.Session;

/// <summary>
/// Defines a Socket.IO session.
/// </summary>
public interface ISession : IMyObserver<ProtocolMessage>, IMyObservable<IMessage>, IMyObserver<IMessage>
{
    /// <summary>
    /// Gets the count of pending binary messages awaiting delivery.
    /// </summary>
    int PendingDeliveryCount { get; }

    /// <summary>
    /// Gets or sets the session options.
    /// </summary>
    SessionOptions Options { get; set; }

    /// <summary>
    /// Gets or sets the action to invoke when the connection is lost.
    /// </summary>
    Action OnDisconnected { get; set; }

    /// <summary>
    /// Sends data over the session.
    /// </summary>
    Task SendAsync(object[] data, CancellationToken cancellationToken);

    /// <summary>
    /// Sends data with a packet identifier over the session.
    /// </summary>
    Task SendAsync(object[] data, int packetId, CancellationToken cancellationToken);

    /// <summary>
    /// Sends acknowledgement data over the session.
    /// </summary>
    Task SendAckDataAsync(object[] data, int packetId, CancellationToken cancellationToken);

    /// <summary>
    /// Connects the session.
    /// </summary>
    Task ConnectAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Disconnects the session.
    /// </summary>
    Task DisconnectAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Sets the opened message from a previous transport for upgrade scenarios.
    /// </summary>
    void SetOpenedMessage(OpenedMessage message);
}
