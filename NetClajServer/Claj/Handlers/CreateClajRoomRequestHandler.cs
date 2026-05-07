using Microsoft.Extensions.Logging;
using NetClajServer.Claj.PacketHandling;
using NetClajServer.Packets;
using NetClajServer.Packets.Claj;

namespace NetClajServer.Claj.Handlers;

public class CreateClajRoomRequestHandler : IPacketHandler<RoomCreateRequestPacket>
{
    public async Task HandleAsync(PacketContext context, RoomCreateRequestPacket packet)
    {
        var serverVersion = new Version(2, 0, 0);
        var remoteVersion = new Version(packet.Version);

        var versionResult = remoteVersion.CompareTo(serverVersion);

        if (versionResult < 0)
        {
            // Client version is too old, deny link creation
            await context.Connection.SendTcp(new ClajMessagePacket()
            {
                Message = "Your CLaJ version is outdated, please update it by reinstalling the 'claj' mod."
            });
            
            context.Connection.Close(ConnectionCloseReason.Closed);
            return;
        }

        long roomId;
        do
        {
            roomId = Random.Shared.NextInt64(long.MinValue, long.MaxValue);
        } while (context.Server.Rooms.ContainsKey(roomId));

        var room = new Room(roomId, context.Connection);
        context.Server.Rooms.TryAdd(room.Id, room);
        context.Logger.LogInformation("Created room {roomId} ({roomIdStr}) for host {connectionId}", room.Id, room.IdString, context.Connection.Id);

        await context.Connection.SendTcp(new RoomLinkPacket()
        {
            RoomId = room.Id
        });
    }
}