using System.Threading;
using System.Threading.Tasks;

namespace Ave.Extensions.SocketIO.Client.Infrastructure;

/// <summary>
/// Abstracts async delay operations.
/// </summary>
public interface IDelay
{
    /// <summary>
    /// Delays for the specified number of milliseconds.
    /// </summary>
    Task DelayAsync(int ms, CancellationToken token);
}
