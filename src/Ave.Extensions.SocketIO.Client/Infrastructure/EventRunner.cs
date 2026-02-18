using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ave.Extensions.SocketIO.Client.Infrastructure;

/// <summary>
/// Invokes event handlers on background threads.
/// </summary>
public class EventRunner : IEventRunner
{
    /// <inheritdoc />
    public void RunInBackground(EventHandler? handler, object sender, EventArgs args)
    {
        if (handler == null)
        {
            return;
        }
        _ = Task.Run(() => handler(sender, args), CancellationToken.None).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void RunInBackground<T>(EventHandler<T>? handler, object sender, T args)
    {
        if (handler == null)
        {
            return;
        }
        _ = Task.Run(() => handler(sender, args), CancellationToken.None).ConfigureAwait(false);
    }
}
