using System.Threading.Tasks;

namespace Ave.Extensions.SocketIO.Client.Observers;

/// <summary>
/// Provides a mechanism for receiving push-based notifications.
/// </summary>
public interface IMyObserver<in T>
{
    /// <summary>
    /// Provides the observer with new data.
    /// </summary>
    Task OnNextAsync(T message);
}
