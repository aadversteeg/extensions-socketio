using System.Threading.Tasks;
using Ave.Extensions.SocketIO.Messages;

namespace Ave.Extensions.SocketIO.Client.Session.EngineIOAdapter;

/// <summary>
/// Handles HTTP long-polling.
/// </summary>
public interface IPollingHandler
{
    /// <summary>
    /// Starts the polling loop.
    /// </summary>
    void StartPolling(OpenedMessage message, bool autoUpgrade);

    /// <summary>
    /// Waits for the HTTP adapter to be ready.
    /// </summary>
    Task WaitHttpAdapterReady();
}
