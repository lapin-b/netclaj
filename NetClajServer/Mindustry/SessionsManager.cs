using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using Microsoft.Extensions.Logging;
using NetClajServer.Claj;
using NetClajServer.Claj.PacketHandling;
using NetClajServer.Packets;

namespace NetClajServer.Mindustry;

public class SessionsManager
{
    private readonly ILogger<SessionsManager> _logger;
    
    private readonly ConcurrentDictionary<int, Connection> _connections = new();
    private readonly ConcurrentDictionary<IPEndPoint, int> _udpEndpointToConnectionId = new();
    private readonly ConcurrentDictionary<long, Room> _rooms = new();

    public ReadOnlyDictionary<long, Room> Rooms => _rooms.AsReadOnly();
    
    public SessionsManager(ILogger<SessionsManager> logger)
    {
        _logger = logger;
    }

    // Connection handling
    
    public void AddAndStartConnection(Connection connection, CancellationToken serverToken)
    {
        _connections.TryAdd(connection.Id, connection);
        connection.Start(serverToken);
    }

    public bool TryRegisterUdpEndpointToConnection(
        IPEndPoint endpoint, 
        int connectionId,
        [NotNullWhen(true)]
        out Connection? connection
    )
    {
        if (!_connections.TryGetValue(connectionId, out connection))
        {
            // Connection ID doesn't exist
            return false;
        }

        if (connection.UdpEndpoint != null)
        {
            // UDP endpoint is already tied with another endpoint
            return false;
        }

        connection.UdpEndpoint = endpoint;
        _udpEndpointToConnectionId.AddOrUpdate(
            endpoint,
            _ => connectionId,
            (_, _) => connectionId
        );

        return true;
    }

    public Connection? GetConnectionByUdpEndpoint(IPEndPoint endpoint)
    {
        return _udpEndpointToConnectionId.TryGetValue(endpoint, out var connectionId)
               && _connections.TryGetValue(connectionId, out var connection)
            ? connection
            : null;
    }

    public Connection? GetConnectionById(int connectionId) => _connections.TryGetValue(connectionId, out var connection) ? connection : null;
    
    public async Task CleanupConnectionState(Connection connection, ArcNetDcReason? disconnectReason)
    {
        _logger.LogInformation("Connection {@Connection} closed with reason {Reason}", connection, disconnectReason);
        
        // Cleanup registry state
        _connections.TryRemove(connection.Id, out _);
        if (connection.UdpEndpoint is { } endpoint)
        {
            _udpEndpointToConnectionId.TryRemove(endpoint, out _);
        }
        
        // Remove the client from a room or close it
        if (connection.ParticipatesInRoomId is { } participatesInRoomId
            && _rooms.TryGetValue(participatesInRoomId, out var room)
           )
        {
            if (room.HostConnectionId == connection.Id)
            {
                _logger.LogInformation("Closing room {@Room} because the host closed the connection", room);
                await room.Close();
                _rooms.TryRemove(room.Id, out _);
            }
            else
            {
                _logger.LogInformation("Connection {@Connection} leaving room {@Room}", connection, room);
                await room.TryLeaveRoom(connection, false, true);
            }
        }
    }
    
    public Room? FindConnectionInRooms(Connection connection)
    {
        return connection.ParticipatesInRoomId is { } participatesInRoomId 
               && _rooms.TryGetValue(participatesInRoomId, out var room) 
            ? room
            : null;
    }
    
    // Room handling
    
    public Room? GetRoom(long roomId) => _rooms.TryGetValue(roomId, out var room) ? room : null;
    
    public void AddRoom(Room room)
    {
        _rooms.TryAdd(room.Id, room);
    }
    
    public async Task CloseRoom(long roomId)
    {
        _rooms.TryGetValue(roomId, out var room);
        if (room == null) return;
        await room.Close();
    }
    
    public Room? CheckRoomExistenceAndOwnership(PacketContext context, ILogger logger)
    {
        if (context.Connection.ParticipatesInRoomId is not { } roomToFetch)
        {
            logger.LogWarning("Connection {connectionID} is not bound to a room", context.Connection.Id);
            return null;
        }

        if (!_rooms.TryGetValue(roomToFetch, out var room))
        {
            // This shouldn't happen if the room registry is kept up to date
            logger.LogError("Room {roomId} doesn't exist", roomToFetch);
            return null;
        }
        
        if (context.Connection.Id != room.HostConnectionId)
        {
            logger.LogWarning("Connection {connectionId} isn't the room owner of room {roomId}", context.Connection.Id, roomToFetch);
            return null;
        }

        return room;
    }

    public async Task CloseAllConnections()
    {
        foreach (var connection in _connections.Values)
        {
            connection.RequestClose(ArcNetDcReason.Closed);
            await connection.Closed;
        }
        
        _connections.Clear();
        _rooms.Clear();
    }
    
    public bool ConnectionIdExists(int id) => _connections.ContainsKey(id);
    public bool RoomIdExists(long id) => _rooms.ContainsKey(id);
}