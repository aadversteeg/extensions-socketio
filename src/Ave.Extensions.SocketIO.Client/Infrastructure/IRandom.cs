namespace Ave.Extensions.SocketIO.Client.Infrastructure;

/// <summary>
/// Abstracts random number generation.
/// </summary>
public interface IRandom
{
    /// <summary>
    /// Returns a non-negative random integer that is less than the specified maximum.
    /// </summary>
    int Next(int max);
}
