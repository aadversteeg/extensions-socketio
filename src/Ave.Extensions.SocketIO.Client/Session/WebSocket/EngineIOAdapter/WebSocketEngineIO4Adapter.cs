using System.Threading;
using System.Threading.Tasks;
using Ave.Extensions.SocketIO.Client.Infrastructure;
using Ave.Extensions.SocketIO.Client.Protocol.WebSocket;
using Ave.Extensions.SocketIO.Client.Session.EngineIOAdapter;
using Ave.Extensions.SocketIO.Protocol;
using Ave.Extensions.SocketIO.Serialization;

namespace Ave.Extensions.SocketIO.Client.Session.WebSocket.EngineIOAdapter;

/// <summary>
/// WebSocket Engine.IO v4 adapter implementation.
/// </summary>
public class WebSocketEngineIO4Adapter : EngineIO4Adapter, IWebSocketEngineIOAdapter
{
    private readonly IWebSocketAdapter _webSocketAdapter;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebSocketEngineIO4Adapter"/> class.
    /// </summary>
    public WebSocketEngineIO4Adapter(
        IStopwatch stopwatch,
        ISerializer serializer,
        IWebSocketAdapter webSocketAdapter) : base(stopwatch, serializer)
    {
        _webSocketAdapter = webSocketAdapter;
    }

    /// <inheritdoc />
    protected override async Task SendConnectAsync(string message)
    {
        using var cts = new CancellationTokenSource(Options.Timeout);
        await _webSocketAdapter.SendAsync(new ProtocolMessage
        {
            Text = message,
        }, cts.Token).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override async Task SendPongAsync()
    {
        using var cts = new CancellationTokenSource(Options.Timeout);
        await _webSocketAdapter.SendAsync(new ProtocolMessage
        {
            Text = "3"
        }, cts.Token).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public byte[] WriteProtocolFrame(byte[] bytes)
    {
        return bytes;
    }

    /// <inheritdoc />
    public byte[] ReadProtocolFrame(byte[] bytes)
    {
        return bytes;
    }
}
