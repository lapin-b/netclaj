using System.Net.Mime;
using Microsoft.Extensions.Logging;
using NetClajServer.Claj.PacketHandling;
using NetClajServer.Packets;
using NetClajServer.Packets.Claj;

namespace NetClajServer.Claj.Handlers;

public class RoomCreateRequestHandler : IPacketHandler<RoomCreationRequestPacket>
{
    private readonly ILogger<RoomCreateRequestHandler> _logger;
    private readonly RoomFactory _roomFactory;
    private const int ServerVersion = 4;

    public RoomCreateRequestHandler(ILogger<RoomCreateRequestHandler> logger, RoomFactory roomFactory)
    {
        _logger = logger;
        _roomFactory = roomFactory;
    }

    public async ValueTask HandleAsync(PacketContext context, RoomCreationRequestPacket packet)
    {
        if (packet.Version != ServerVersion)
        {
            var reason = packet.Version < ServerVersion
                ? ClajConnectionCloseReason.ObsoleteClient
                : ClajConnectionCloseReason.OutdatedServer;
            
            await context.Connection.SendTcp(new RoomClosedPacket()
            {
                Reason = reason
            });

            context.Connection.RequestClose(ArcNetDcReason.Error);
            return;
        }

        if (
            context.Connection.ParticipatesInRoomId is { } roomId
            && context.Server.Rooms.TryGetValue(roomId, out var existingRoom)
            && existingRoom.HostConnectionId == context.Connection.Id
        )
        {
            // This connection is already a host of a room. Send a message
            _logger.LogWarning(
                "Connection {ConnectionId} is already hosting a room {roomId}",
                context.Connection.Id,
                roomId
            );

            await context.Connection.SendTcp(new ClajMessagePacket()
            {
                Message = ClajMessages.AlreadyHosting
            });

            return;
        }

        var room = _roomFactory.Create(
            context.Connection, packet.RoomType,
            id => context.Server.Rooms.ContainsKey(id)
        );
        
        _logger.LogInformation("Created room {roomId} ({roomIdStr}) for host {connectionId}", room.Id, room.IdString, context.Connection.Id);
        context.Server.Rooms.TryAdd(room.Id, room);
        
        await context.Connection.SendTcp(new RoomLinkPacket
        {
            RoomId = room.Id
        });

        await context.Connection.SendTcp(new ClajTextMessagePacket
        {
            Message = "Warning: this CLaJ node is very alpha software although it has been tested. Here be dragons !"
        });
    }
}