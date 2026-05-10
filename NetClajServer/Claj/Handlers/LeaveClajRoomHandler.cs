using Microsoft.Extensions.Logging;
using NetClajServer.Claj.PacketHandling;
using NetClajServer.Packets.Claj;

namespace NetClajServer.Claj.Handlers;

public class LeaveClajRoomHandler: IPacketHandler<ConnectionClosedPacket>
{
    public async Task HandleAsync(PacketContext context, ConnectionClosedPacket packet)
    {
        // Get the room this packet is for
        if (context.Server.FindConnectionInRooms(context.Connection) is not {} room)
        {
            context.Logger.LogWarning("Tried to disconnect from a non-existing room");
            return;
        }

        if (context.Connection.Id != room.HostConnectionId)
        {
            context.Logger.LogWarning("Connection ID {ConnectionID} has tried to close another's player while not being the host", context.Connection.Id);
            return;
        }

        if (context.Server.Connections.TryGetValue(packet.ConnectionId, out var targetConnection))
        {
            context.Logger.LogInformation("Host made {ConnectionID} leave the room {RoomId}", targetConnection.Id, room.Id);
            await room.LeaveRoom(targetConnection);
        }
        else
        {
            context.Logger.LogWarning("Couldn't have {connectionId} leave room {roomId}: connection doesn't exist", packet.ConnectionId, room.Id);
        }
    }
}