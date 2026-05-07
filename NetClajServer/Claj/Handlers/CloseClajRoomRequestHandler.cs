using System.Net.Mime;
using Microsoft.Extensions.Logging;
using NetClajServer.Claj.PacketHandling;
using NetClajServer.Packets.Claj;

namespace NetClajServer.Claj.Handlers;

public class CloseClajRoomRequestHandler: IPacketHandler<RoomCloseRequestPacket>
{
    public async Task HandleAsync(PacketContext context, RoomCloseRequestPacket packet)
    {
        var room = context.Server.Rooms.Values
            .FirstOrDefault(r => r.HostConnectionId == context.Connection.Id);

        if (room == null)
        {
            context.Logger.LogInformation("No host found for {roomId}", room.Id);
            return;
        }

        context.Logger.LogInformation("Closing room {roomId}", room.Id);
        context.Server.Rooms.TryRemove(room.Id, out _);
        // TODO: close room properly
    }
}