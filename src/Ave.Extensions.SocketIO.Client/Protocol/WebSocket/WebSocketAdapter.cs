using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Ave.Extensions.SocketIO.Protocol;

namespace Ave.Extensions.SocketIO.Client.Protocol.WebSocket;

/// <summary>
/// WebSocket transport adapter implementation.
/// </summary>
public class WebSocketAdapter : ProtocolAdapter, IWebSocketAdapter, IDisposable
{
    private readonly ILogger<WebSocketAdapter> _logger;
    private readonly IWebSocketClientAdapter _clientAdapter;
    private readonly CancellationTokenSource _receiveCancellationTokenSource = new CancellationTokenSource();

    /// <summary>
    /// Initializes a new instance of the <see cref="WebSocketAdapter"/> class.
    /// </summary>
    public WebSocketAdapter(ILogger<WebSocketAdapter> logger, IWebSocketClientAdapter clientAdapter)
    {
        _logger = logger;
        _clientAdapter = clientAdapter;
    }

    /// <inheritdoc />
    public async Task ConnectAsync(Uri uri, CancellationToken cancellationToken)
    {
        await _clientAdapter.ConnectAsync(uri, cancellationToken).ConfigureAwait(false);
        var token = _receiveCancellationTokenSource.Token;
        _ = ReceiveAsync(token).ConfigureAwait(false);
    }

    private async Task ReceiveAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var message = await _clientAdapter.ReceiveAsync(cancellationToken).ConfigureAwait(false);
                var protocolMessage = new ProtocolMessage();
                switch (message.Type)
                {
                    case WebSocketMessageType.Text:
                        protocolMessage.Type = ProtocolMessageType.Text;
                        protocolMessage.Text = Encoding.UTF8.GetString(message.Bytes);
                        break;
                    case WebSocketMessageType.Binary:
                        protocolMessage.Type = ProtocolMessageType.Bytes;
                        protocolMessage.Bytes = message.Bytes;
                        break;
                    default:
                        throw new ArgumentException();
                }

                await OnNextAsync(protocolMessage).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to receive message");
                _logger.LogError(e.ToString());
                OnDisconnected();
                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task SendAsync(ProtocolMessage message, CancellationToken cancellationToken)
    {
        var isTextButNull = message.Type == ProtocolMessageType.Text && message.Text == null;
        var isBytesButNull = message.Type == ProtocolMessageType.Bytes && message.Bytes == null;
        if (isTextButNull || isBytesButNull)
        {
            throw new ArgumentNullException();
        }

        _logger.LogDebug("Sending {type} message...", message.Type);
        if (message.Type == ProtocolMessageType.Text)
        {
            var data = Encoding.UTF8.GetBytes(message.Text!);
            await _clientAdapter.SendAsync(data, WebSocketMessageType.Text, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await _clientAdapter
                .SendAsync(message.Bytes!, WebSocketMessageType.Binary, cancellationToken)
                .ConfigureAwait(false);
        }
        _logger.LogDebug("Sent {type} message.", message.Type);
    }

    /// <inheritdoc />
    public override void SetDefaultHeader(string name, string value) => _clientAdapter.SetDefaultHeader(name, value);

    /// <summary>
    /// Disposes resources.
    /// </summary>
    public void Dispose()
    {
        _receiveCancellationTokenSource.Cancel();
        _receiveCancellationTokenSource.Dispose();
    }
}
