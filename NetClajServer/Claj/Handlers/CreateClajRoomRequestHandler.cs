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
            
            await context.Connection.CloseAsync(ConnectionCloseReason.Error);
            return;
        }

        if (
            context.Connection.ParticipatesInRoomId is { } roomId
            && context.Server.Rooms.TryGetValue(roomId, out var existingRoom)
            && existingRoom.HostConnectionId == context.Connection.Id
        )
        {
            // This connection is already a host of a room. Ignore the room creation request
            context.Logger.LogWarning(
                "Connection {ConnectionId} is already hosting a room {roomId}",
                context.Connection.Id,
                roomId
            );

            return;
        }

        var room = new Room(context.Connection);
        do
        {
            room.Id = Random.Shared.NextInt64(long.MinValue, long.MaxValue);
        } while (room.Id == 0 || !context.Server.Rooms.TryAdd(room.Id, room));
        room.Open();

        context.Logger.LogInformation("Created room {roomId} ({roomIdStr}) for host {connectionId}", room.Id, room.IdString, context.Connection.Id);
        
        await context.Connection.SendTcp(new RoomLinkPacket
        {
            RoomId = room.Id
        });

        await context.Connection.SendTcp(new ClajMessagePacket
        {
            Message = "Warning: this CLaJ node is very alpha software although it has been tested. Here be dragons !"
        });
    }
}