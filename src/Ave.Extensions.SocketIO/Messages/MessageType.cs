namespace Ave.Extensions.SocketIO.Messages;

/// <summary>
/// Defines the Socket.IO/Engine.IO message types and their numeric protocol values.
/// </summary>
public enum MessageType
{
    /// <summary>
    /// Engine.IO opened message (handshake).
    /// </summary>
    Opened,

    /// <summary>
    /// Engine.IO close message. Sent by either side to end the session.
    /// </summary>
    Close,

    /// <summary>
    /// Ping keep-alive message.
    /// </summary>
    Ping,

    /// <summary>
    /// Pong response message.
    /// </summary>
    Pong,

    /// <summary>
    /// Socket.IO connected message.
    /// </summary>
    Connected = 40,

    /// <summary>
    /// Socket.IO disconnected message.
    /// </summary>
    Disconnected,

    /// <summary>
    /// Socket.IO event message.
    /// </summary>
    Event,

    /// <summary>
    /// Socket.IO acknowledgement message.
    /// </summary>
    Ack,

    /// <summary>
    /// Socket.IO error message.
    /// </summary>
    Error,

    /// <summary>
    /// Socket.IO binary event message.
    /// </summary>
    Binary,

    /// <summary>
    /// Socket.IO binary acknowledgement message.
    /// </summary>
    BinaryAck,

    /// <summary>
    /// Engine.IO upgrade message. Sent by the client to complete a transport upgrade.
    /// </summary>
    Upgrade = 5,

    /// <summary>
    /// Engine.IO noop message. Used during transport upgrade to flush the buffer.
    /// </summary>
    Noop = 6,
}
