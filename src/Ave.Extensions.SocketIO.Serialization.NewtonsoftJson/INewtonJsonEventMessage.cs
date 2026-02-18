using Ave.Extensions.SocketIO.Messages;

namespace Ave.Extensions.SocketIO.Serialization.NewtonsoftJson
{
    /// <summary>
    /// Defines a Newtonsoft.Json-based event message.
    /// </summary>
    public interface INewtonJsonEventMessage : INewtonJsonAckMessage, IEventMessage
    {
    }
}
