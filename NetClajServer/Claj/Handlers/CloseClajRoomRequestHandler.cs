using System.Net.Mime;
using Microsoft.Extensions.Logging;
using NetClajServer.Claj.PacketHandling;
using NetClajServer.Packets.Claj;

namespace NetClajServer.Claj.Handlers;

public class CloseClajRoomRequestHandler: IPacketHandler<RoomCloseRequestPacket>
{
    public async Task HandleAsync(PacketContext context, RoomCloseRequestPacket packet)
    {
        if (
            !context.Server.ConnectionIdToRoomParticipation.TryGetValue(context.Connection.Id, out var roomId)
        )
        {
            context.Logger.LogWarning("Connection {ConnectionID} is not bound to any room", context.Connection.Id);
            return;
        }

        if (!context.Server.Rooms.TryGetValue(roomId, out var room))
        {
            context.Logger.LogError("Room ID {roomId} does not exist", roomId);
            // TODO: clean server bookkeeping map
            return;
        }

        if (room.HostConnectionId != context.Connection.Id)
        {
            context.Logger.LogWarning("Connection {ConnectionID} tried to close a room it didn't host ({hostConnectionId} does)", context.Connection.Id, room.HostConnectionId);
        }

        context.Logger.LogInformation("Closing room {roomId} because host closed it", room.Id);
        await room.CloseRoom();

        foreach (var mapping in context.Server.ConnectionIdToRoomParticipation.Where(kv => kv.Value == room.Id))
        {
            context.Server.ConnectionIdToRoomParticipation.TryRemove(mapping);
        }
        
        context.Server.Rooms.TryRemove(room.Id, out _);
    }
}