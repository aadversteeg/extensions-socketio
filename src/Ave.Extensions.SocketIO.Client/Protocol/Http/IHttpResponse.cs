using System.Threading.Tasks;

namespace Ave.Extensions.SocketIO.Client.Protocol.Http;

/// <summary>
/// Represents an HTTP response.
/// </summary>
public interface IHttpResponse
{
    /// <summary>
    /// Gets the media type of the response content.
    /// </summary>
    string? MediaType { get; }

    /// <summary>
    /// Reads the response content as a byte array.
    /// </summary>
    Task<byte[]> ReadAsByteArrayAsync();

    /// <summary>
    /// Reads the response content as a string.
    /// </summary>
    Task<string> ReadAsStringAsync();
}
