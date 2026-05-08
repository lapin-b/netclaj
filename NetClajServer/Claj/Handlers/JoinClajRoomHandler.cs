using System.Net.Mime;
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
            context.Connection.Close(ConnectionCloseReason.Error);
            return;
        }

        if (
            context.Server.ConnectionIdToRoomParticipation.TryRemove(context.Connection.Id, out var alreadyJoinedRoomId)
            && context.Server.Rooms.TryGetValue(alreadyJoinedRoomId, out var alreadyJoinedRoom)
        )
        {
            await alreadyJoinedRoom.LeaveRoom(context.Connection, true);
        }

        context.Server.ConnectionIdToRoomParticipation.TryAdd(context.Connection.Id, roomToJoin.Id);
        await roomToJoin.JoinRoom(context.Connection);
    }
}