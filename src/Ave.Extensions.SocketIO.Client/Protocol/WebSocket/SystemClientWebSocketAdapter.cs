using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using SysWebSocketMessageType = System.Net.WebSockets.WebSocketMessageType;

namespace Ave.Extensions.SocketIO.Client.Protocol.WebSocket;

/// <summary>
/// Adapter for <see cref="IWebSocketClient"/> that handles chunked send/receive.
/// </summary>
public class SystemClientWebSocketAdapter : IWebSocketClientAdapter
{
    private readonly IWebSocketClient _ws;

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemClientWebSocketAdapter"/> class.
    /// </summary>
    public SystemClientWebSocketAdapter(IWebSocketClient ws)
    {
        _ws = ws;
    }

    /// <summary>
    /// Gets or sets the send chunk size in bytes. Default is 8192.
    /// </summary>
    public int SendChunkSize { get; set; } = 1024 * 8;

    /// <summary>
    /// Gets or sets the receive chunk size in bytes. Default is 8192.
    /// </summary>
    public int ReceiveChunkSize { get; set; } = 1024 * 8;

    /// <inheritdoc />
    public async Task SendAsync(byte[] data, WebSocketMessageType messageType, CancellationToken cancellationToken)
    {
        var type = (SysWebSocketMessageType)messageType;
        var offset = 0;
        bool endOfMessage;
        do
        {
            var length = SendChunkSize;
            endOfMessage = offset + SendChunkSize >= data.Length;
            if (endOfMessage)
            {
                length = data.Length - offset;
            }

            var segment = new ArraySegment<byte>(data, offset, length);
            offset += length;
            await _ws.SendAsync(segment, type, endOfMessage, cancellationToken).ConfigureAwait(false);
        } while (!endOfMessage);
    }

    /// <inheritdoc />
    public Task ConnectAsync(Uri uri, CancellationToken cancellationToken)
    {
        return _ws.ConnectAsync(uri, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WebSocketMessage> ReceiveAsync(CancellationToken cancellationToken)
    {
        var bytes = new byte[ReceiveChunkSize];
        var buffer = new MemoryStream();

        WebSocketReceiveResult result;
        do
        {
            result = await _ws.ReceiveAsync(new ArraySegment<byte>(bytes), cancellationToken).ConfigureAwait(false);
            await buffer.WriteAsync(bytes, 0, result.Count, cancellationToken).ConfigureAwait(false);
        } while (!result.EndOfMessage);

        return new WebSocketMessage
        {
            Bytes = buffer.ToArray(),
            Type = (WebSocketMessageType)result.MessageType,
        };
    }

    /// <inheritdoc />
    public void SetDefaultHeader(string name, string value) => _ws.SetDefaultHeader(name, value);
}
