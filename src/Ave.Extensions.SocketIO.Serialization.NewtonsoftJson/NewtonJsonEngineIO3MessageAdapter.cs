using Newtonsoft.Json.Linq;
using Ave.Extensions.SocketIO.Messages;

namespace Ave.Extensions.SocketIO.Serialization.NewtonsoftJson
{
    /// <summary>
    /// Engine.IO v3 message adapter using Newtonsoft.Json.
    /// </summary>
    public class NewtonJsonEngineIO3MessageAdapter : IEngineIOMessageAdapter
    {
        /// <inheritdoc />
        public ConnectedMessage DeserializeConnectedMessage(string text)
        {
            var message = new ConnectedMessage();
            if (!string.IsNullOrEmpty(text))
            {
                message.Namespace = text.TrimEnd(',');
            }
            return message;
        }

        /// <inheritdoc />
        public ErrorMessage DeserializeErrorMessage(string text)
        {
            var error = JToken.Parse(text).ToObject<string>()!;
            return new ErrorMessage
            {
                Error = error,
            };
        }
    }
}
