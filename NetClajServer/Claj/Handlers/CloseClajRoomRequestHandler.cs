using System.Net.Mime;
using Microsoft.Extensions.Logging;
using NetClajServer.Claj.PacketHandling;
using NetClajServer.Packets.Claj;

namespace NetClajServer.Claj.Handlers;

public class CloseClajRoomRequestHandler: IPacketHandler<RoomCloseRequestPacket>
{
    public async Task HandleAsync(PacketContext context, RoomCloseRequestPacket packet)
    {
        if (context.Server.FindConnectionInRooms(context.Connection) is not {} room)
        {
            context.Logger.LogWarning("Connection {ConnectionID} is not bound to any room", context.Connection.Id);
            return;
        }

        if (room.HostConnectionId != context.Connection.Id)
        {
            context.Logger.LogWarning(
                "Connection {ConnectionID} tried to close a room it didn't host ({hostConnectionId} does)", 
                context.Connection.Id, 
                room.HostConnectionId
            );
            return;
        }

        context.Logger.LogInformation("Closing room {roomId} because host closed it", room.Id);
        context.Server.Rooms.TryRemove(room.Id, out _);
        await room.CloseRoom();
    }
}