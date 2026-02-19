using System;
using System.Security.Cryptography;

namespace Ave.Extensions.SocketIO.Server.EngineIO;

/// <summary>
/// Generates cryptographically random base64url-encoded session identifiers.
/// </summary>
public class SessionIdGenerator : ISessionIdGenerator
{
    private const int ByteLength = 15;

    /// <inheritdoc />
    public string Generate()
    {
        var bytes = new byte[ByteLength];
#if NETSTANDARD2_1
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
#else
        RandomNumberGenerator.Fill(bytes);
#endif
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}
