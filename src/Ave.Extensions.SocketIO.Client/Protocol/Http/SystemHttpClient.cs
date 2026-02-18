using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Ave.Extensions.SocketIO.Client.Protocol.Http;

/// <summary>
/// System.Net.Http.HttpClient-based implementation of <see cref="IHttpClient"/>.
/// </summary>
public class SystemHttpClient : IHttpClient
{
    private readonly HttpClient _http;

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemHttpClient"/> class.
    /// </summary>
    public SystemHttpClient(HttpClient http)
    {
        _http = http;
    }

    /// <inheritdoc />
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <inheritdoc />
    public void SetDefaultHeader(string name, string value)
    {
        _http.DefaultRequestHeaders.TryAddWithoutValidation(name, value);
    }

    /// <inheritdoc />
    public async Task<IHttpResponse> SendAsync(HttpRequest req, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(new HttpMethod(req.Method.ToString()), req.Uri);
        request.Content = req.BodyType switch
        {
            RequestBodyType.Text => new StringContent(req.BodyText ?? string.Empty),
            RequestBodyType.Bytes => new ByteArrayContent(req.BodyBytes!),
            _ => throw new NotSupportedException(),
        };

        SetHeaders(req, request);

        var res = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        return new SystemHttpResponse(res);
    }

    private static void SetHeaders(HttpRequest req, HttpRequestMessage request)
    {
        var content = (ByteArrayContent)request.Content!;
        foreach (var header in req.Headers)
        {
            if (HttpHeaders.ContentType.Equals(header.Key))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue(header.Value);
                continue;
            }
            request.Headers.Add(header.Key, header.Value);
        }
    }
}
