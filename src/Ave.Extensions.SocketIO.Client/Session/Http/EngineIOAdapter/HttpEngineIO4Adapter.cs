using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ave.Extensions.SocketIO.Client.Infrastructure;
using Ave.Extensions.SocketIO.Client.Protocol.Http;
using Ave.Extensions.SocketIO.Client.Session.EngineIOAdapter;
using Ave.Extensions.SocketIO.Messages;
using Ave.Extensions.SocketIO.Protocol;
using Ave.Extensions.SocketIO.Serialization;

namespace Ave.Extensions.SocketIO.Client.Session.Http.EngineIOAdapter;

/// <summary>
/// HTTP Engine.IO v4 adapter implementation.
/// </summary>
public class HttpEngineIO4Adapter : EngineIO4Adapter, IHttpEngineIOAdapter
{
    private const string Delimiter = "\u001E";
    private readonly IHttpAdapter _httpAdapter;
    private readonly IRetriable _retryPolicy;
    private readonly IPollingHandler _pollingHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpEngineIO4Adapter"/> class.
    /// </summary>
    public HttpEngineIO4Adapter(
        IStopwatch stopwatch,
        IHttpAdapter httpAdapter,
        IRetriable retryPolicy,
        ISerializer serializer,
        IDelay delay,
        IPollingHandler pollingHandler) : base(stopwatch, serializer, delay)
    {
        _httpAdapter = httpAdapter;
        _retryPolicy = retryPolicy;
        _pollingHandler = pollingHandler;
    }

    /// <inheritdoc />
    protected override async Task SendConnectAsync(string message)
    {
        var req = ToHttpRequest(message);
        using var cts = new CancellationTokenSource(Options.Timeout);
        await _httpAdapter.SendAsync(req, cts.Token).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override async Task SendPongAsync()
    {
        await _retryPolicy.RetryAsync(3, async () =>
        {
            var pong = ToHttpRequest("3");
            using var cts = new CancellationTokenSource(Options.Timeout);
            await _httpAdapter.SendAsync(pong, cts.Token).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override void OnOpenedMessageReceived(OpenedMessage message)
    {
        _pollingHandler.StartPolling(message, Options.AutoUpgrade);
    }

    /// <inheritdoc />
    public HttpRequest ToHttpRequest(ICollection<byte[]> bytes)
    {
        if (!bytes.Any())
        {
            throw new ArgumentException("The array cannot be empty");
        }

        var req = new HttpRequest
        {
            Method = RequestMethod.Post,
            BodyType = RequestBodyType.Text,
        };

        var base64Strings = bytes.Select(b => $"b{Convert.ToBase64String(b)}");
        req.BodyText = string.Join(Delimiter, base64Strings);
        return req;
    }

    /// <inheritdoc />
    public HttpRequest ToHttpRequest(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            throw new ArgumentException("The content cannot be null or empty");
        }

        return new HttpRequest
        {
            Method = RequestMethod.Post,
            BodyType = RequestBodyType.Text,
            BodyText = content,
        };
    }

    /// <inheritdoc />
    public IEnumerable<ProtocolMessage> ExtractMessagesFromText(string text)
    {
        var items = text.Split(new[] { Delimiter }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var item in items)
        {
            if (item[0] == 'b')
            {
                var bytes = Convert.FromBase64String(item.Substring(1));
                yield return new ProtocolMessage
                {
                    Type = ProtocolMessageType.Bytes,
                    Bytes = bytes,
                };
            }
            else
            {
                yield return new ProtocolMessage
                {
                    Type = ProtocolMessageType.Text,
                    Text = item,
                };
            }
        }
    }

    /// <inheritdoc />
    public IEnumerable<ProtocolMessage> ExtractMessagesFromBytes(byte[] bytes)
    {
        return new List<ProtocolMessage>();
    }
}
