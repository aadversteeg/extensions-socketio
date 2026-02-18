namespace Ave.Extensions.SocketIO.Client.Observers;

/// <summary>
/// Defines a provider for push-based notification.
/// </summary>
public interface IMyObservable<out T>
{
    /// <summary>
    /// Notifies the provider that an observer is to receive notifications.
    /// </summary>
    void Subscribe(IMyObserver<T> observer);

    /// <summary>
    /// Notifies the provider that an observer is to stop receiving notifications.
    /// </summary>
    void Unsubscribe(IMyObserver<T> observer);
}
