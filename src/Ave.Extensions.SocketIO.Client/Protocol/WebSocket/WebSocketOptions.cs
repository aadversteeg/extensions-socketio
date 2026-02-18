using System.Net;
using System.Net.Security;

namespace Ave.Extensions.SocketIO.Client.Protocol.WebSocket;

/// <summary>
/// Configuration options for the WebSocket client.
/// </summary>
public class WebSocketOptions
{
    /// <summary>
    /// Gets or sets the proxy.
    /// </summary>
    public IWebProxy? Proxy { get; set; }

    /// <summary>
    /// Gets or sets the remote certificate validation callback.
    /// </summary>
    public RemoteCertificateValidationCallback? RemoteCertificateValidationCallback { get; set; }
}
