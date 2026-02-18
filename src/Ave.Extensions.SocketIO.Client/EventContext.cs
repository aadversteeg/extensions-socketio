using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ave.Extensions.SocketIO.Messages;

namespace Ave.Extensions.SocketIO.Client;

/// <summary>
/// Default implementation of <see cref="IEventContext"/> that wraps a data message.
/// </summary>
public class EventContext : IEventContext
{
    private readonly IDataMessage _message;
    private readonly IInternalSocketIOClient _io;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventContext"/> class.
    /// </summary>
    public EventContext(IDataMessage message, IInternalSocketIOClient io)
    {
        _message = message;
        _io = io;
    }

    /// <inheritdoc />
    public async Task SendAckDataAsync(IEnumerable<object> data, CancellationToken cancellationToken)
    {
        await _io.SendAckDataAsync(_message.Id, data, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public string RawText => _message.RawText;

    /// <inheritdoc />
    public async Task SendAckDataAsync(IEnumerable<object> data)
    {
        await SendAckDataAsync(data, CancellationToken.None).ConfigureAwait(false);
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
}
