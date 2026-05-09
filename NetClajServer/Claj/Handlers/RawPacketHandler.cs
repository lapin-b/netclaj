using Microsoft.Extensions.Logging;
using NetClajServer.Claj.PacketHandling;
using NetClajServer.Packets.Claj;
using NetClajServer.Packets.Framework;

namespace NetClajServer.Claj.Handlers;

public class RawPacketHandler: IPacketHandler<GamePacket>, IPacketHandler<ClajPayloadWrapping>
{
    public Task HandleAsync(PacketContext context, GamePacket packet)
    {
        if (context.Connection.ParticipatesInRoomId is not { } participatesInRoomId)
        {
            // Ignore the packet if not participating in a room
            context.Logger.LogWarning(
                "{ConnectionId} is not yet participating in a room and is sending raw packets",
                context.Connection.Id
            );
            return Task.CompletedTask;
        }

        if (!context.Server.Rooms.TryGetValue(participatesInRoomId, out var room))
        {
            context.Logger.LogWarning(
                "Connection {ConnectionId} says it is participating in room {roomId} but it doesn't exist",
                context.Connection.Id,
                participatesInRoomId
            );

            context.Connection.ParticipatesInRoomId = null;

            return Task.CompletedTask;
        }

        return room.HandlePacket(context, packet);
    }

    public Task HandleAsync(PacketContext context, ClajPayloadWrapping packet)
    {
        if (context.Server.FindConnectionInRooms(context.Connection) is not { } room)
        {
            context.Logger.LogWarning("Connection is partaking in room {roomId} but it doesn't exist", context.Connection.ParticipatesInRoomId);
            return Task.CompletedTask;
        }

        if (context.Connection.Id != room.HostConnectionId)
        {
            context.Logger.LogWarning("Received a Claj wrapping packet not from room host connection. Dropping");
            return Task.CompletedTask;
        }

        return room.HandlePacket(context, packet);
    }
}