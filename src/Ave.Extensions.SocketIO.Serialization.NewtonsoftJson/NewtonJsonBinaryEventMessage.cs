using Ave.Extensions.SocketIO.Messages;

namespace Ave.Extensions.SocketIO.Serialization.NewtonsoftJson
{
    /// <summary>
    /// Binary event message using Newtonsoft.Json for deserialization.
    /// </summary>
    public class NewtonJsonBinaryEventMessage : NewtonJsonBinaryAckMessage, INewtonJsonEventMessage
    {
        /// <inheritdoc />
        public override MessageType Type => MessageType.Binary;

        /// <summary>
        /// Gets or sets the event name.
        /// </summary>
        public string Event { get; set; } = null!;
    }
}
