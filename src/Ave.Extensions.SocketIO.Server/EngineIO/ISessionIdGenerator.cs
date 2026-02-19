namespace Ave.Extensions.SocketIO.Server.EngineIO;

/// <summary>
/// Generates unique session identifiers for Engine.IO sessions.
/// </summary>
public interface ISessionIdGenerator
{
    /// <summary>
    /// Generates a new unique session identifier.
    /// </summary>
    string Generate();
}
