using Microsoft.Extensions.Logging;
using NetClajServer.Claj.PacketHandling;
using NetClajServer.Packets.Claj;
using NetClajServer.Packets.Framework;

namespace NetClajServer.Claj.Handlers;

public class GamePacketHandler: IPacketHandler<GamePacket>, IPacketHandler<ClajPayloadWrapping>
{
    private readonly ILogger<GamePacketHandler> _logger;

    public GamePacketHandler(ILogger<GamePacketHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(PacketContext context, GamePacket packet)
    {
        if (context.Connection.ParticipatesInRoomId is not { } participatesInRoomId)
        {
            // Ignore the packet if not participating in a room
            _logger.LogWarning(
                "{ConnectionId} is not yet participating in a room and raw packets are already trying to flow through handlers. Dropping",
                context.Connection.Id
            );
            return Task.CompletedTask;
        }

        if (context.Server.Rooms.TryGetValue(participatesInRoomId, out var room))
        {
            return room.HandlePacket(context, packet);
        }
        
        // The room class owns ParticipatesInRoomId management.
        // This should NOT happen.
        _logger.LogError(
            "Connection {ConnectionId} says it is participating in room {roomId} but it doesn't exist",
            context.Connection.Id,
            participatesInRoomId
        );

        // The only time a handler can edit the participating room id to reflect reality
        // is when it is pointing to a room that doesn't exist.
        context.Connection.ParticipatesInRoomId = null;

        return Task.CompletedTask;
    }

    public Task HandleAsync(PacketContext context, ClajPayloadWrapping packet)
    {
        if (context.Server.FindConnectionInRooms(context.Connection) is not { } room)
        {
            _logger.LogWarning("Connection is partaking in room {roomId} but it doesn't exist", context.Connection.ParticipatesInRoomId);
            return Task.CompletedTask;
        }

        if (context.Connection.Id != room.HostConnectionId)
        {
            _logger.LogWarning("Received a Claj wrapping packet not from room host connection. Dropping");
            return Task.CompletedTask;
        }

        return room.HandlePacket(context, packet);
    }
}