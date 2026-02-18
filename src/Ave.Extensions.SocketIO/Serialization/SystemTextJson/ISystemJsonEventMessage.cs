using Ave.Extensions.SocketIO.Messages;

namespace Ave.Extensions.SocketIO.Serialization.SystemTextJson;

/// <summary>
/// Internal interface for System.Text.Json event message implementation.
/// </summary>
public interface ISystemJsonEventMessage : ISystemJsonAckMessage, IEventMessage
{
}
