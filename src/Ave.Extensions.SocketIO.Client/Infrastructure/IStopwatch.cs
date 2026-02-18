using System;

namespace Ave.Extensions.SocketIO.Client.Infrastructure;

/// <summary>
/// Abstracts stopwatch functionality.
/// </summary>
public interface IStopwatch
{
    /// <summary>
    /// Gets the total elapsed time.
    /// </summary>
    TimeSpan Elapsed { get; }

    /// <summary>
    /// Stops time interval measurement, resets, and starts measuring.
    /// </summary>
    void Restart();

    /// <summary>
    /// Stops measuring elapsed time.
    /// </summary>
    void Stop();
}
