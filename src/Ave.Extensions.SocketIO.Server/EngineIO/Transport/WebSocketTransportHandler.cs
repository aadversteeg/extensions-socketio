using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Ave.Extensions.SocketIO.Protocol;
using Ave.Extensions.SocketIO.Server.EngineIO.Codec;

namespace Ave.Extensions.SocketIO.Server.EngineIO.Transport;

/// <summary>
/// Handles WebSocket transport for Engine.IO sessions.
/// </summary>
public class WebSocketTransportHandler : IWebSocketTransportHandler
{
    private readonly ILogger<WebSocketTransportHandler> _logger;
    private readonly WebSocketFrameCodecV3 _frameCodecV3;
    private readonly WebSocketFrameCodecV4 _frameCodecV4;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebSocketTransportHandler"/> class.
    /// </summary>
    public WebSocketTransportHandler(
        ILogger<WebSocketTransportHandler> logger,
        WebSocketFrameCodecV3 frameCodecV3,
        WebSocketFrameCodecV4 frameCodecV4)
    {
        _logger = logger;
        _frameCodecV3 = frameCodecV3;
        _frameCodecV4 = frameCodecV4;
    }

    /// <inheritdoc />
    public async Task HandleAsync(WebSocket webSocket, IEngineIOSession session, CancellationToken cancellationToken)
    {
        var engineSession = (EngineIOSession)session;
        engineSession.WebSocketSend = (msg, ct) => SendWebSocketMessage(webSocket, session.Version, msg, ct);

        await RunReceiveLoopAsync(webSocket, session, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task HandleUpgradeAsync(WebSocket webSocket, IEngineIOSession session, CancellationToken cancellationToken)
    {
        // Step 1: Read probe ping from client ("2probe")
        var probeMessage = await ReceiveTextAsync(webSocket, cancellationToken).ConfigureAwait(false);
        if (probeMessage != "2probe")
        {
            _logger.LogWarning("Expected '2probe' during upgrade but got '{Message}'", probeMessage);
            return;
        }

        // Step 2: Send probe pong ("3probe")
        await SendTextAsync(webSocket, "3probe", cancellationToken).ConfigureAwait(false);

        // Step 3: Read upgrade packet ("5")
        var upgradeMessage = await ReceiveTextAsync(webSocket, cancellationToken).ConfigureAwait(false);
        if (upgradeMessage != "5")
        {
            _logger.LogWarning("Expected '5' (upgrade) during upgrade but got '{Message}'", upgradeMessage);
            return;
        }

        // Step 4: Send noop to flush polling ("6")
        await session.SendAsync("6", cancellationToken).ConfigureAwait(false);

        // Step 5: Complete upgrade
        session.UpgradeTransport();
        var engineSession = (EngineIOSession)session;
        engineSession.WebSocketSend = (msg, ct) => SendWebSocketMessage(webSocket, session.Version, msg, ct);

        _logger.LogDebug("Transport upgraded to WebSocket for session {Sid}", session.Sid);

        // Continue with normal WebSocket message loop
        await RunReceiveLoopAsync(webSocket, session, cancellationToken).ConfigureAwait(false);
    }

    private async Task RunReceiveLoopAsync(WebSocket webSocket, IEngineIOSession session, CancellationToken cancellationToken)
    {
        var buffer = new byte[64 * 1024];
        var frameCodec = GetFrameCodec(session.Version);

        try
        {
            while (webSocket.State == WebSocketState.Open && session.IsOpen && !cancellationToken.IsCancellationRequested)
            {
                var result = await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer), cancellationToken).ConfigureAwait(false);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await session.CloseAsync().ConfigureAwait(false);
                    if (webSocket.State == WebSocketState.CloseReceived)
                    {
                        await webSocket.CloseOutputAsync(
                            WebSocketCloseStatus.NormalClosure, "", cancellationToken).ConfigureAwait(false);
                    }
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var text = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    await session.ReceiveAsync(new ProtocolMessage
                    {
                        Type = ProtocolMessageType.Text,
                        Text = text,
                    }).ConfigureAwait(false);
                }
                else if (result.MessageType == WebSocketMessageType.Binary)
                {
                    var raw = new byte[result.Count];
                    Buffer.BlockCopy(buffer, 0, raw, 0, result.Count);
                    var data = frameCodec.ReadFrame(raw);
                    await session.ReceiveAsync(new ProtocolMessage
                    {
                        Type = ProtocolMessageType.Bytes,
                        Bytes = data,
                    }).ConfigureAwait(false);
                }
            }
        }
        catch (WebSocketException ex)
        {
            _logger.LogDebug(ex, "WebSocket error for session {Sid}", session.Sid);
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }
        finally
        {
            if (session.IsOpen)
            {
                await session.CloseAsync().ConfigureAwait(false);
            }
        }
    }

    private async Task SendWebSocketMessage(
        WebSocket webSocket, EngineIOVersion version, ProtocolMessage message, CancellationToken cancellationToken)
    {
        if (webSocket.State != WebSocketState.Open) return;

        if (message.Type == ProtocolMessageType.Text && message.Text != null)
        {
            var bytes = Encoding.UTF8.GetBytes(message.Text);
            await webSocket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                cancellationToken).ConfigureAwait(false);
        }
        else if (message.Type == ProtocolMessageType.Bytes && message.Bytes != null)
        {
            var frameCodec = GetFrameCodec(version);
            var framedBytes = frameCodec.WriteFrame(message.Bytes);
            await webSocket.SendAsync(
                new ArraySegment<byte>(framedBytes),
                WebSocketMessageType.Binary,
                true,
                cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task<string> ReceiveTextAsync(WebSocket webSocket, CancellationToken cancellationToken)
    {
        var buffer = new byte[1024];
        var result = await webSocket.ReceiveAsync(
            new ArraySegment<byte>(buffer), cancellationToken).ConfigureAwait(false);
        return Encoding.UTF8.GetString(buffer, 0, result.Count);
    }

    private static async Task SendTextAsync(WebSocket webSocket, string text, CancellationToken cancellationToken)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        await webSocket.SendAsync(
            new ArraySegment<byte>(bytes),
            WebSocketMessageType.Text,
            true,
            cancellationToken).ConfigureAwait(false);
    }

    private IWebSocketFrameCodec GetFrameCodec(EngineIOVersion version)
    {
        return version == EngineIOVersion.V3 ? (IWebSocketFrameCodec)_frameCodecV3 : _frameCodecV4;
    }
}
