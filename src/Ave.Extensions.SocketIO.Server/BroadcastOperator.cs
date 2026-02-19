using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ave.Extensions.SocketIO.Server.Rooms;

namespace Ave.Extensions.SocketIO.Server;

/// <summary>
/// Fluent builder for broadcasting events to targeted rooms.
/// </summary>
public class BroadcastOperator : IBroadcastOperator
{
    private readonly Namespace _namespace;
    private readonly IRoomManager _roomManager;
    private readonly HashSet<string> _targetRooms = new HashSet<string>();
    private readonly HashSet<string> _excludeRooms = new HashSet<string>();
    private readonly HashSet<string> _excludeSocketIds = new HashSet<string>();

    /// <summary>
    /// Initializes a new instance of the <see cref="BroadcastOperator"/> class.
    /// </summary>
    internal BroadcastOperator(Namespace ns, IRoomManager roomManager)
    {
        _namespace = ns;
        _roomManager = roomManager;
    }

    /// <summary>
    /// Adds a socket ID to the exclusion set.
    /// </summary>
    internal BroadcastOperator WithExclude(string socketId)
    {
        _excludeSocketIds.Add(socketId);
        return this;
    }

    /// <inheritdoc />
    public IBroadcastOperator To(string room)
    {
        _targetRooms.Add(room);
        return this;
    }

    /// <inheritdoc />
    public IBroadcastOperator To(IEnumerable<string> rooms)
    {
        foreach (var room in rooms)
        {
            _targetRooms.Add(room);
        }
        return this;
    }

    /// <inheritdoc />
    public IBroadcastOperator Except(string room)
    {
        _excludeRooms.Add(room);
        return this;
    }

    /// <inheritdoc />
    public IBroadcastOperator Except(IEnumerable<string> rooms)
    {
        foreach (var room in rooms)
        {
            _excludeRooms.Add(room);
        }
        return this;
    }

    /// <inheritdoc />
    public async Task EmitAsync(string eventName, IEnumerable<object> data)
    {
        var socketIds = ResolveTargetSocketIds();
        foreach (var socketId in socketIds)
        {
            var socket = _namespace.GetSocket(socketId);
            if (socket != null)
            {
                await socket.EmitAsync(eventName, data).ConfigureAwait(false);
            }
        }
    }

    /// <inheritdoc />
    public async Task EmitAsync(string eventName)
    {
        await EmitAsync(eventName, Enumerable.Empty<object>()).ConfigureAwait(false);
    }

    private HashSet<string> ResolveTargetSocketIds()
    {
        HashSet<string> targetIds;

        if (_targetRooms.Count > 0)
        {
            // Union of all target rooms
            targetIds = new HashSet<string>();
            foreach (var room in _targetRooms)
            {
                foreach (var id in _roomManager.GetSocketIds(room))
                {
                    targetIds.Add(id);
                }
            }
        }
        else
        {
            // All sockets in namespace
            targetIds = new HashSet<string>(_namespace.SocketIds);
        }

        // Exclude rooms
        foreach (var room in _excludeRooms)
        {
            foreach (var id in _roomManager.GetSocketIds(room))
            {
                targetIds.Remove(id);
            }
        }

        // Exclude individual sockets
        foreach (var id in _excludeSocketIds)
        {
            targetIds.Remove(id);
        }

        return targetIds;
    }
}
