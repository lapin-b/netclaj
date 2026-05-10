using Microsoft.Extensions.Logging;
using NetClajServer.Claj.PacketHandling;
using NetClajServer.Packets;
using NetClajServer.Packets.Claj;

namespace NetClajServer.Claj.Handlers;

public class JoinClajRoomHandler: IPacketHandler<RoomJoinPacket>
{
    public async Task HandleAsync(PacketContext context, RoomJoinPacket packet)
    {
        if (!context.Server.Rooms.TryGetValue(packet.RoomId, out var roomToJoin))
        {
            context.Logger.LogWarning(
                "Connection {connectionId} tried to join a non-existing room ID {roomId}",
                context.Connection.Id,
                packet.RoomId
            );
            
            await context.Connection.CloseAsync(ConnectionCloseReason.Error);
            return;
        }

        if (context.Server.FindConnectionInRooms(context.Connection) is {} alreadyJoinedRoom)
        {
            await alreadyJoinedRoom.TryLeaveRoom(context.Connection, true);
        }

        context.Logger.LogInformation("{ConnectionId} joining room {roomId}", context.Connection.Id, roomToJoin.Id);
        context.Connection.ParticipatesInRoomId = roomToJoin.Id;
        await roomToJoin.TryJoinRoom(context.Connection);
        while (context.Connection.RawPacketsQueue.TryDequeue(out var pendingPacket))
        {
            context.Logger.LogDebug("Submitting packets from {ConnectionId} into the room", context.Connection.Id);
            await roomToJoin.HandlePacket(context, pendingPacket);
        }
    }
}