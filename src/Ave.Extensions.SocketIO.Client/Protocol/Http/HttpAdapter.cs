using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Ave.Extensions.SocketIO.Protocol;

namespace Ave.Extensions.SocketIO.Client.Protocol.Http;

/// <summary>
/// HTTP transport adapter implementation.
/// </summary>
public class HttpAdapter : ProtocolAdapter, IHttpAdapter
{
    private readonly IHttpClient _httpClient;
    private readonly ILogger<HttpAdapter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpAdapter"/> class.
    /// </summary>
    public HttpAdapter(IHttpClient httpClient, ILogger<HttpAdapter> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public Uri? Uri { get; set; }

    /// <inheritdoc />
    public bool IsReadyToSend => Uri is not null && Uri.Query.Contains("sid=");

    private static async Task<ProtocolMessage> GetMessageAsync(IHttpResponse response)
    {
        var message = new ProtocolMessage();
        if (MediaTypeNames.Application.Octet.Equals(response.MediaType, StringComparison.InvariantCultureIgnoreCase))
        {
            message.Type = ProtocolMessageType.Bytes;
            message.Bytes = await response.ReadAsByteArrayAsync().ConfigureAwait(false);
        }
        else
        {
            message.Type = ProtocolMessageType.Text;
            message.Text = await response.ReadAsStringAsync().ConfigureAwait(false);
        }
        return message;
    }

    private async Task HandleResponseAsync(IHttpResponse response)
    {
        var incomingMessage = await GetMessageAsync(response).ConfigureAwait(false);
        await OnNextAsync(incomingMessage).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SendAsync(HttpRequest req, CancellationToken cancellationToken)
    {
        req.Uri = req.Uri ?? NewUri();
        try
        {
            var response = await _httpClient.SendAsync(req, cancellationToken).ConfigureAwait(false);
            var body = req.BodyType == RequestBodyType.Text ? req.BodyText : $"binary {req.BodyBytes!.Length}";
            _logger.LogDebug("[Polling] {Body}", body);
            _ = HandleResponseAsync(response).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _logger.LogError("Failed to send http request");
            _logger.LogError(e.ToString());
            if (!req.IsConnect)
            {
                OnDisconnected();
            }
            throw;
        }
    }

    private Uri NewUri()
    {
        var str = $"{Uri!.AbsoluteUri}&t={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        return new Uri(str);
    }

    /// <inheritdoc />
    public override void SetDefaultHeader(string name, string value) => _httpClient.SetDefaultHeader(name, value);
}
