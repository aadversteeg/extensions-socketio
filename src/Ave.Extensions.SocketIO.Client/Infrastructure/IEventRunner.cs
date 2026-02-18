using System;

namespace Ave.Extensions.SocketIO.Client.Infrastructure;

/// <summary>
/// Abstracts event invocation on background threads.
/// </summary>
public interface IEventRunner
{
    /// <summary>
    /// Invokes an event handler on a background thread.
    /// </summary>
    void RunInBackground(EventHandler? handler, object sender, EventArgs args);

    /// <summary>
    /// Invokes a generic event handler on a background thread.
    /// </summary>
    void RunInBackground<T>(EventHandler<T>? handler, object sender, T args);
}
