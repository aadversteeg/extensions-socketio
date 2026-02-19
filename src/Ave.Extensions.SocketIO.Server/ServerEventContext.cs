using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ave.Extensions.SocketIO.Messages;

namespace Ave.Extensions.SocketIO.Server;

/// <summary>
/// Server-side event context wrapping a data message with typed access and ack support.
/// </summary>
public class ServerEventContext : IServerEventContext
{
    private readonly IDataMessage _message;
    private readonly Func<int, object[], CancellationToken, Task>? _sendAck;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServerEventContext"/> class.
    /// </summary>
    public ServerEventContext(IDataMessage message, Func<int, object[], CancellationToken, Task>? sendAck)
    {
        _message = message;
        _sendAck = sendAck;
    }

    /// <inheritdoc />
    public T? GetValue<T>(int index)
    {
        return _message.GetValue<T>(index);
    }

    /// <inheritdoc />
    public object? GetValue(Type type, int index)
    {
        return _message.GetValue(type, index);
    }

    /// <inheritdoc />
    public async Task SendAckDataAsync(IEnumerable<object> data)
    {
        await SendAckDataAsync(data, CancellationToken.None).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SendAckDataAsync(IEnumerable<object> data, CancellationToken cancellationToken)
    {
        if (_sendAck != null && _message.Id >= 0)
        {
            await _sendAck(_message.Id, data.ToArray(), cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public string RawText => _message.RawText;
}
