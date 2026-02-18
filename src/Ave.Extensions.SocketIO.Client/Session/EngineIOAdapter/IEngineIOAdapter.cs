using System;
using System.Threading.Tasks;
using Ave.Extensions.SocketIO.Client.Observers;
using Ave.Extensions.SocketIO.Messages;

namespace Ave.Extensions.SocketIO.Client.Session.EngineIOAdapter;

/// <summary>
/// Defines an Engine.IO protocol adapter.
/// </summary>
public interface IEngineIOAdapter : IMyObservable<IMessage>
{
    /// <summary>
    /// Gets or sets the adapter options.
    /// </summary>
    EngineIOAdapterOptions Options { get; set; }

    /// <summary>
    /// Gets or sets the action to invoke when the Engine.IO connection should be closed.
    /// </summary>
    Action? OnDisconnected { get; set; }

    /// <summary>
    /// Processes an incoming message and returns whether it should be swallowed.
    /// </summary>
    Task<bool> ProcessMessageAsync(IMessage message);
}
