using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Ave.Extensions.SocketIO.Client.Infrastructure;
using Ave.Extensions.SocketIO.Client.Protocol.WebSocket;
using Ave.Extensions.SocketIO.Client.Session.EngineIOAdapter;
using Ave.Extensions.SocketIO.Protocol;

namespace Ave.Extensions.SocketIO.Client.Session.WebSocket.EngineIOAdapter;

/// <summary>
/// WebSocket Engine.IO v3 adapter implementation.
/// </summary>
public class WebSocketEngineIO3Adapter : EngineIO3Adapter, IWebSocketEngineIOAdapter
{
    private readonly IWebSocketAdapter _webSocketAdapter;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebSocketEngineIO3Adapter"/> class.
    /// </summary>
    public WebSocketEngineIO3Adapter(
        IStopwatch stopwatch,
        ILogger<WebSocketEngineIO3Adapter> logger,
        IWebSocketAdapter webSocketAdapter,
        IDelay delay) : base(stopwatch, logger, delay)
    {
        _webSocketAdapter = webSocketAdapter;
    }

    /// <inheritdoc />
    protected override async Task SendConnectAsync()
    {
        using var cts = new CancellationTokenSource(Options.Timeout);
        await _webSocketAdapter.SendAsync(new ProtocolMessage
        {
            Text = $"40{Options.Namespace},"
        }, cts.Token).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override async Task SendPingAsync()
    {
        using var cts = new CancellationTokenSource(Options.Timeout);
        await _webSocketAdapter.SendAsync(new ProtocolMessage
        {
            Text = "2"
        }, cts.Token).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public byte[] WriteProtocolFrame(byte[] bytes)
    {
        byte[] buffer = new byte[bytes.Length + 1];
        buffer[0] = 4;
        Buffer.BlockCopy(bytes, 0, buffer, 1, bytes.Length);
        return buffer;
    }

    /// <inheritdoc />
    public byte[] ReadProtocolFrame(byte[] bytes)
    {
        var result = new byte[bytes.Length - 1];
        Buffer.BlockCopy(bytes, 1, result, 0, result.Length);
        return result;
    }
}
