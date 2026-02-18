using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Ave.Extensions.SocketIO.Client.Infrastructure;
using Ave.Extensions.SocketIO.Client.Protocol.Http;
using Ave.Extensions.SocketIO.Client.Session.EngineIOAdapter;
using Ave.Extensions.SocketIO.Messages;
using Ave.Extensions.SocketIO.Protocol;

namespace Ave.Extensions.SocketIO.Client.Session.Http.EngineIOAdapter;

/// <summary>
/// HTTP Engine.IO v3 adapter implementation.
/// </summary>
public class HttpEngineIO3Adapter : EngineIO3Adapter, IHttpEngineIOAdapter
{
    private readonly IHttpAdapter _httpAdapter;
    private readonly IRetriable _retryPolicy;
    private readonly ILogger<HttpEngineIO3Adapter> _logger;
    private readonly IPollingHandler _pollingHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpEngineIO3Adapter"/> class.
    /// </summary>
    public HttpEngineIO3Adapter(
        IStopwatch stopwatch,
        IHttpAdapter httpAdapter,
        IRetriable retryPolicy,
        ILogger<HttpEngineIO3Adapter> logger,
        IPollingHandler pollingHandler,
        IDelay delay) : base(stopwatch, logger, delay)
    {
        _httpAdapter = httpAdapter;
        _retryPolicy = retryPolicy;
        _logger = logger;
        _pollingHandler = pollingHandler;
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
            BodyType = RequestBodyType.Bytes,
            Headers = new Dictionary<string, string>
            {
                { HttpHeaders.ContentType, MediaTypeNames.Application.Octet },
            },
        };
        var capacity = bytes.Sum(x => x.Length + 16);
        var list = new List<byte>(capacity);
        foreach (var b in bytes)
        {
            list.Add(1);
            list.AddRange((b.Length + 1).ToString().Select(c => Convert.ToByte(c - '0')));
            list.Add(byte.MaxValue);
            list.Add(4);
            list.AddRange(b);
        }
        req.BodyBytes = list.ToArray();
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
            BodyText = $"{content.Length}:{content}",
        };
    }

    /// <inheritdoc />
    public IEnumerable<ProtocolMessage> ExtractMessagesFromText(string text)
    {
        var p = 0;
        while (true)
        {
            var index = text.IndexOf(':', p);
            if (index == -1)
            {
                break;
            }
            var lengthStr = text.Substring(p, index - p);
            if (int.TryParse(lengthStr, out var length))
            {
                var msg = text.Substring(index + 1, length);
                yield return new ProtocolMessage { Text = msg };
            }
            else
            {
                break;
            }
            p = index + length + 1;
            if (p >= text.Length)
            {
                break;
            }
        }
    }

    /// <inheritdoc />
    public IEnumerable<ProtocolMessage> ExtractMessagesFromBytes(byte[] bytes)
    {
        var index = 0;
        while (index < bytes.Length)
        {
            byte messageType = bytes[index];
            index++;
            var payloadLength = 0;

            while (index < bytes.Length && bytes[index] != byte.MaxValue)
            {
                payloadLength = payloadLength * 10 + bytes[index++];
            }

            index++;

            if (messageType == byte.MinValue)
            {
                var data = new byte[payloadLength];
                Buffer.BlockCopy(bytes, index, data, 0, data.Length);
                yield return new ProtocolMessage
                {
                    Type = ProtocolMessageType.Text,
                    Text = Encoding.UTF8.GetString(data),
                };
            }
            else
            {
                var data = new byte[payloadLength - 1];
                Buffer.BlockCopy(bytes, index + 1, data, 0, data.Length);
                yield return new ProtocolMessage
                {
                    Type = ProtocolMessageType.Bytes,
                    Bytes = data,
                };
            }

            index += payloadLength;
        }
    }

    /// <inheritdoc />
    protected override async Task SendConnectAsync()
    {
        var req = ToHttpRequest($"40{Options.Namespace},");
        using var cts = new CancellationTokenSource(Options.Timeout);
        await _httpAdapter.SendAsync(req, cts.Token).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override async Task SendPingAsync()
    {
        var request = ToHttpRequest("2");
        await _retryPolicy.RetryAsync(3, async () =>
        {
            using var cts = new CancellationTokenSource(Options.Timeout);
            await _httpAdapter.SendAsync(request, cts.Token).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override async Task OnPingTaskStarted()
    {
        _logger.LogDebug("[StartPingAsync] Waiting for HttpAdapter ready...");
        await _pollingHandler.WaitHttpAdapterReady().ConfigureAwait(false);
        _logger.LogDebug("[StartPingAsync] HttpAdapter is ready");
    }

    /// <inheritdoc />
    protected override void OnOpenedMessageReceived(OpenedMessage message)
    {
        _pollingHandler.StartPolling(message, Options.AutoUpgrade);
    }
}
