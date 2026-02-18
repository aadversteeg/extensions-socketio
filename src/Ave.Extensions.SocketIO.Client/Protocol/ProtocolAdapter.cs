using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ave.Extensions.SocketIO.Client.Observers;
using Ave.Extensions.SocketIO.Protocol;

namespace Ave.Extensions.SocketIO.Client.Protocol;

/// <summary>
/// Abstract base class for protocol adapters with observer pattern support.
/// </summary>
public abstract class ProtocolAdapter : IProtocolAdapter
{
    private readonly List<IMyObserver<ProtocolMessage>> _observers = new List<IMyObserver<ProtocolMessage>>();

    /// <inheritdoc />
    public void Subscribe(IMyObserver<ProtocolMessage> observer)
    {
        if (_observers.Contains(observer))
        {
            return;
        }
        _observers.Add(observer);
    }

    /// <inheritdoc />
    public void Unsubscribe(IMyObserver<ProtocolMessage> observer)
    {
        _observers.Remove(observer);
    }

    /// <inheritdoc />
    public async Task OnNextAsync(ProtocolMessage message)
    {
        foreach (var observer in _observers)
        {
            await observer.OnNextAsync(message).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public abstract void SetDefaultHeader(string name, string value);

    /// <inheritdoc />
    public Action OnDisconnected { get; set; } = null!;
}
