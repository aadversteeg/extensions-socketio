using Ave.Extensions.SocketIO.Messages;

namespace Ave.Extensions.SocketIO.Serialization.NewtonsoftJson
{
    /// <summary>
    /// Event message using Newtonsoft.Json for deserialization.
    /// </summary>
    public class NewtonJsonEventMessage : NewtonJsonAckMessage, INewtonJsonEventMessage
    {
        /// <inheritdoc />
        public override MessageType Type => MessageType.Event;

        /// <summary>
        /// Gets or sets the event name.
        /// </summary>
        public string Event { get; set; } = null!;
    }
}
