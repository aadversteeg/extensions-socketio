using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ave.Extensions.SocketIO.Client.Protocol.Http;

/// <summary>
/// Abstracts HTTP client operations.
/// </summary>
public interface IHttpClient
{
    /// <summary>
    /// Gets or sets the request timeout.
    /// </summary>
    TimeSpan Timeout { get; set; }

    /// <summary>
    /// Sets a default header for all requests.
    /// </summary>
    void SetDefaultHeader(string name, string value);

    /// <summary>
    /// Sends an HTTP request.
    /// </summary>
    Task<IHttpResponse> SendAsync(HttpRequest req, CancellationToken cancellationToken);
}
