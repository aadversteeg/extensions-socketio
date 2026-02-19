using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Ave.Extensions.SocketIO.Server.Rooms;

/// <summary>
/// Thread-safe in-memory room manager with bidirectional indexing.
/// </summary>
public class RoomManager : IRoomManager
{
    private static readonly IReadOnlyCollection<string> EmptySet = new HashSet<string>();

    private readonly ConcurrentDictionary<string, HashSet<string>> _roomToSockets =
        new ConcurrentDictionary<string, HashSet<string>>();

    private readonly ConcurrentDictionary<string, HashSet<string>> _socketToRooms =
        new ConcurrentDictionary<string, HashSet<string>>();

    private readonly object _lock = new object();

    /// <inheritdoc />
    public void Join(string socketId, string room)
    {
        lock (_lock)
        {
            var rooms = _socketToRooms.GetOrAdd(socketId, _ => new HashSet<string>());
            rooms.Add(room);

            var sockets = _roomToSockets.GetOrAdd(room, _ => new HashSet<string>());
            sockets.Add(socketId);
        }
    }

    /// <inheritdoc />
    public void Join(string socketId, IEnumerable<string> rooms)
    {
        foreach (var room in rooms)
        {
            Join(socketId, room);
        }
    }

    /// <inheritdoc />
    public void Leave(string socketId, string room)
    {
        lock (_lock)
        {
            if (_socketToRooms.TryGetValue(socketId, out var rooms))
            {
                rooms.Remove(room);
            }

            if (_roomToSockets.TryGetValue(room, out var sockets))
            {
                sockets.Remove(socketId);
                if (sockets.Count == 0)
                {
                    _roomToSockets.TryRemove(room, out _);
                }
            }
        }
    }

    /// <inheritdoc />
    public void LeaveAll(string socketId)
    {
        lock (_lock)
        {
            if (_socketToRooms.TryRemove(socketId, out var rooms))
            {
                foreach (var room in rooms)
                {
                    if (_roomToSockets.TryGetValue(room, out var sockets))
                    {
                        sockets.Remove(socketId);
                        if (sockets.Count == 0)
                        {
                            _roomToSockets.TryRemove(room, out _);
                        }
                    }
                }
            }
        }
    }

    /// <inheritdoc />
    public IReadOnlyCollection<string> GetRooms(string socketId)
    {
        lock (_lock)
        {
            if (_socketToRooms.TryGetValue(socketId, out var rooms))
            {
                return new HashSet<string>(rooms);
            }
            return EmptySet;
        }
    }

    /// <inheritdoc />
    public IReadOnlyCollection<string> GetSocketIds(string room)
    {
        lock (_lock)
        {
            if (_roomToSockets.TryGetValue(room, out var sockets))
            {
                return new HashSet<string>(sockets);
            }
            return EmptySet;
        }
    }
}
