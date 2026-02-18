using System.Threading;
using System.Threading.Tasks;

namespace Ave.Extensions.SocketIO.Client.Infrastructure;

/// <summary>
/// Task.Delay-based implementation of <see cref="IDelay"/>.
/// </summary>
public class TaskDelay : IDelay
{
    /// <inheritdoc />
    public async Task DelayAsync(int ms, CancellationToken cancellationToken)
    {
        await Task.Delay(ms, cancellationToken).ConfigureAwait(false);
    }
}
