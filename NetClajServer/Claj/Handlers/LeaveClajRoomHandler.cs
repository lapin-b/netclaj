using Microsoft.Extensions.Logging;
using NetClajServer.Claj.PacketHandling;
using NetClajServer.Packets.Claj;

namespace NetClajServer.Claj.Handlers;

public class LeaveClajRoomHandler: IPacketHandler<ConnectionClosedPacket>
{
    public async Task HandleAsync(PacketContext context, ConnectionClosedPacket packet)
    {
        // Get the room this packet is for
        if (
            !context.Server.ConnectionIdToRoomParticipation.TryGetValue(context.Connection.Id, out var roomId)
            || !context.Server.Rooms.TryGetValue(roomId, out var room)
        )
        {
            return;
        }

        // Check if the host disconnected the target connection from the room
        if (context.Connection.Id != room.Id)
        {
            context.Logger.LogWarning("Connection ID {ConnectionID} has tried to close another's player while not being the host", context.Connection.Id);
            return;
        }

        context.Server.ConnectionIdToRoomParticipation.TryRemove(packet.ConnectionId, out _);
        if (context.Server.Connections.TryGetValue(packet.ConnectionId, out var targetConnection))
        {
            await room.LeaveRoom(targetConnection);
        }
    }
}