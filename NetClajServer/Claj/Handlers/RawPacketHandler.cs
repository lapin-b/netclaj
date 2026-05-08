using Microsoft.Extensions.Logging;
using NetClajServer.Claj.PacketHandling;
using NetClajServer.Packets.Framework;

namespace NetClajServer.Claj.Handlers;

public class RawPacketHandler: IPacketHandler<RawPacket>
{
    public async Task HandleAsync(PacketContext context, RawPacket packet)
    {
        if (context.Connection.ParticipatesInRoomId is not { } participatesInRoomId)
        {
            // Ignore the packet if not participating in a room
            return;
        }

        if (!context.Server.Rooms.TryGetValue(participatesInRoomId, out var room))
        {
            context.Logger.LogWarning(
                "Connection {ConnectionId} says it is participating in room {roomId} but it doesn't exist",
                context.Connection.Id,
                participatesInRoomId
            );

            context.Connection.ParticipatesInRoomId = null;

            return;
        }

        await room.HandlePacket(context, packet);
    }
}