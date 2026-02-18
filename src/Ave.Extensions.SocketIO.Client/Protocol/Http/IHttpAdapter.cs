using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ave.Extensions.SocketIO.Client.Protocol.Http;

/// <summary>
/// Defines an HTTP transport adapter.
/// </summary>
public interface IHttpAdapter : IProtocolAdapter
{
    /// <summary>
    /// Sends an HTTP request.
    /// </summary>
    Task SendAsync(HttpRequest req, CancellationToken cancellationToken);

    /// <summary>
    /// Gets or sets the base URI.
    /// </summary>
    Uri? Uri { get; set; }

    /// <summary>
    /// Gets a value indicating whether the adapter is ready to send requests.
    /// </summary>
    bool IsReadyToSend { get; }
}
