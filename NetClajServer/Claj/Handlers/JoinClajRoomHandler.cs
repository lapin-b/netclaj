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
            
            context.Connection.Close(ConnectionCloseReason.Error);
            return;
        }

        if (context.Server.FindConnectionInRooms(context.Connection) is {} alreadyJoinedRoom)
        {
            await alreadyJoinedRoom.LeaveRoom(context.Connection, true);
        }

        await roomToJoin.JoinRoom(context.Connection);
    }
}