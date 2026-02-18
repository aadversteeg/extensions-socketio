using System;
using System.Diagnostics;

namespace Ave.Extensions.SocketIO.Client.Infrastructure;

/// <summary>
/// System.Diagnostics.Stopwatch-based implementation of <see cref="IStopwatch"/>.
/// </summary>
public class SystemStopwatch : IStopwatch
{
    private readonly Stopwatch _stopwatch = new Stopwatch();

    /// <inheritdoc />
    public TimeSpan Elapsed => _stopwatch.Elapsed;

    /// <inheritdoc />
    public void Restart() => _stopwatch.Restart();

    /// <inheritdoc />
    public void Stop() => _stopwatch.Stop();
}
