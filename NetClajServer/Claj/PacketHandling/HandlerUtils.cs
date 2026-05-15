using Microsoft.Extensions.Logging;

namespace NetClajServer.Claj.PacketHandling;

public static class HandlerUtils
{
    public static Room? CheckRoomExistenceAndOwnership(PacketContext context, ILogger logger)
    {
        if (context.Connection.ParticipatesInRoomId is not { } roomToFetch)
        {
            logger.LogWarning("Connection {connectionID} is not bound to a room", context.Connection.Id);
            return null;
        }

        if (!context.Server.Rooms.TryGetValue(roomToFetch, out var room))
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
}