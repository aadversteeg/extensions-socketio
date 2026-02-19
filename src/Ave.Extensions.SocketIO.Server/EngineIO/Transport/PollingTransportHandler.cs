using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ave.Extensions.SocketIO.Server.EngineIO.Codec;

namespace Ave.Extensions.SocketIO.Server.EngineIO.Transport;

/// <summary>
/// Handles HTTP long-polling transport for Engine.IO sessions.
/// </summary>
public class PollingTransportHandler : IPollingTransportHandler
{
    private readonly IPayloadCodec _v3Codec;
    private readonly IPayloadCodec _v4Codec;

    /// <summary>
    /// Initializes a new instance of the <see cref="PollingTransportHandler"/> class.
    /// </summary>
    public PollingTransportHandler(EngineIO3PayloadCodec v3Codec, EngineIO4PayloadCodec v4Codec)
    {
        _v3Codec = v3Codec;
        _v4Codec = v4Codec;
    }

    /// <inheritdoc />
    public async Task HandleGetAsync(HttpContext context, IEngineIOSession session, CancellationToken cancellationToken)
    {
        var codec = GetCodec(session.Version);

        try
        {
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            var messages = await session.DrainAsync(linkedCts.Token).ConfigureAwait(false);

            if (messages.Length == 0)
            {
                context.Response.StatusCode = 200;
                context.Response.ContentType = "text/plain; charset=UTF-8";
                return;
            }

            var payload = codec.Encode(messages);
            context.Response.StatusCode = 200;
            context.Response.ContentType = "text/plain; charset=UTF-8";
            await context.Response.WriteAsync(payload, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Timeout or client disconnected — return empty response
            context.Response.StatusCode = 200;
            context.Response.ContentType = "text/plain; charset=UTF-8";
        }
    }

    /// <inheritdoc />
    public async Task HandlePostAsync(HttpContext context, IEngineIOSession session, CancellationToken cancellationToken)
    {
        var codec = GetCodec(session.Version);
        string body;

        using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8))
        {
            body = await reader.ReadToEndAsync().ConfigureAwait(false);
        }

        if (!string.IsNullOrEmpty(body))
        {
            var messages = codec.Decode(body);
            foreach (var message in messages)
            {
                await session.ReceiveAsync(message).ConfigureAwait(false);
            }
        }

        context.Response.StatusCode = 200;
        context.Response.ContentType = "text/plain; charset=UTF-8";
        await context.Response.WriteAsync("ok", cancellationToken).ConfigureAwait(false);
    }

    private IPayloadCodec GetCodec(EngineIOVersion version)
    {
        return version == EngineIOVersion.V3 ? _v3Codec : _v4Codec;
    }
}
