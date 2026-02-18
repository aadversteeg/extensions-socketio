namespace Ave.Extensions.SocketIO.Messages;

/// <summary>
/// Represents a binary acknowledgement message with both binary data and typed data access.
/// </summary>
public interface IBinaryAckMessage : IBinaryMessage, IDataMessage
{
}
