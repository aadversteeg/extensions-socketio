using System.Net.Http;
using System.Threading.Tasks;

namespace Ave.Extensions.SocketIO.Client.Protocol.Http;

/// <summary>
/// System.Net.Http-based implementation of <see cref="IHttpResponse"/>.
/// </summary>
public class SystemHttpResponse : IHttpResponse
{
    private readonly HttpResponseMessage _response;

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemHttpResponse"/> class.
    /// </summary>
    public SystemHttpResponse(HttpResponseMessage response)
    {
        _response = response;
    }

    /// <inheritdoc />
    public string? MediaType => _response.Content.Headers.ContentType?.MediaType;

    /// <inheritdoc />
    public async Task<byte[]> ReadAsByteArrayAsync()
    {
        return await _response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<string> ReadAsStringAsync()
    {
        return await _response.Content.ReadAsStringAsync().ConfigureAwait(false);
    }
}
