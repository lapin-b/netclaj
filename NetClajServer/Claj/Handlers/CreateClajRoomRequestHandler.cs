using System.Net.Mime;
using Microsoft.Extensions.Logging;
using NetClajServer.Claj.PacketHandling;
using NetClajServer.Packets;
using NetClajServer.Packets.Claj;

namespace NetClajServer.Claj.Handlers;

public class CreateClajRoomRequestHandler : IPacketHandler<RoomCreateRequestPacket>
{
    private static readonly Version ServerVersion = new(2, 0, 0);
    
    public async Task HandleAsync(PacketContext context, RoomCreateRequestPacket packet)
    {
        var remoteVersion = new Version(packet.Version);
        if (remoteVersion.CompareTo(ServerVersion) < 0)
        {
            // Client version is too old, deny link creation by closing the connection
            await context.Connection.SendTcp(new ClajMessagePacket()
            {
                Message = "Your CLaJ version is outdated, please update it by reinstalling the 'claj' mod."
            });
            
            context.Connection.Close(ConnectionCloseReason.Closed);
            return;
        }
        
        // TODO: handle if the connection is already in or hosting a room

        var room = new Room(0, context.Connection);
        do
        {
            room.Id = Random.Shared.NextInt64(long.MinValue, long.MaxValue);
        } while (!context.Server.Rooms.TryAdd(room.Id, room));

        context.Logger.LogInformation("Created room {roomId} ({roomIdStr}) for host {connectionId}", room.Id, room.IdString, context.Connection.Id);

        await context.Connection.SendTcp(new RoomLinkPacket()
        {
            RoomId = room.Id
        });

        context.Server.ConnectionIdToRoomParticipation.TryAdd(context.Connection.Id, room.Id);
    }
}