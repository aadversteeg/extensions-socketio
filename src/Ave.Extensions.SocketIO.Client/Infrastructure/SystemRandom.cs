using System;

namespace Ave.Extensions.SocketIO.Client.Infrastructure;

/// <summary>
/// System.Random-based implementation of <see cref="IRandom"/>.
/// </summary>
public class SystemRandom : IRandom
{
    private readonly Random _random = new Random();

    /// <inheritdoc />
    public int Next(int max)
    {
        return _random.Next(max);
    }
}
