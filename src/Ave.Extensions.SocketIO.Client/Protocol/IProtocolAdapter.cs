using System;
using Ave.Extensions.SocketIO.Client.Observers;
using Ave.Extensions.SocketIO.Protocol;

namespace Ave.Extensions.SocketIO.Client.Protocol;

/// <summary>
/// Defines a protocol adapter that can observe and publish protocol messages.
/// </summary>
public interface IProtocolAdapter : IMyObservable<ProtocolMessage>, IMyObserver<ProtocolMessage>
{
    /// <summary>
    /// Sets a default header for all requests.
    /// </summary>
    void SetDefaultHeader(string name, string value);

    /// <summary>
    /// Gets or sets the action to invoke when the connection is lost.
    /// </summary>
    Action OnDisconnected { get; set; }
}
