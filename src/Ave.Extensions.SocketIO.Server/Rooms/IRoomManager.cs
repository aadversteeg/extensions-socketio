using System.Collections.Generic;

namespace Ave.Extensions.SocketIO.Server.Rooms;

/// <summary>
/// Manages room membership for sockets within a namespace.
/// </summary>
public interface IRoomManager
{
    /// <summary>
    /// Adds a socket to a room.
    /// </summary>
    void Join(string socketId, string room);

    /// <summary>
    /// Adds a socket to multiple rooms.
    /// </summary>
    void Join(string socketId, IEnumerable<string> rooms);

    /// <summary>
    /// Removes a socket from a room.
    /// </summary>
    void Leave(string socketId, string room);

    /// <summary>
    /// Removes a socket from all rooms.
    /// </summary>
    void LeaveAll(string socketId);

    /// <summary>
    /// Gets the set of rooms a socket belongs to.
    /// </summary>
    IReadOnlyCollection<string> GetRooms(string socketId);

    /// <summary>
    /// Gets the set of socket IDs in a room.
    /// </summary>
    IReadOnlyCollection<string> GetSocketIds(string room);
}
