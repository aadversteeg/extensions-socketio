using System.Threading.Tasks;
using Ave.Extensions.SocketIO.Messages;
using Ave.Extensions.SocketIO.Server.EngineIO;

namespace Ave.Extensions.SocketIO.Server;

/// <summary>
/// Routes deserialized Socket.IO messages to the appropriate namespace and socket.
/// </summary>
public interface IMessageRouter
{
    /// <summary>
    /// Routes a message from the specified Engine.IO session.
    /// </summary>
    Task RouteAsync(IMessage message, IEngineIOSession session, IHandshake handshake);
}
