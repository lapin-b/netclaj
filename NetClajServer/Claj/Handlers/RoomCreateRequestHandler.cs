using Microsoft.Extensions.Logging;
using NetClajServer.Claj.PacketHandling;
using NetClajServer.Mindustry;
using PacketHandling;
using PacketHandling.Claj;
using PacketHandling.Framework;
using PacketHandling.Support;

namespace NetClajServer.Claj.Handlers;

public class RoomCreateRequestHandler : IPacketHandler<RoomCreationRequestPacket>
{
    private readonly ILogger<RoomCreateRequestHandler> _logger;
    private readonly SessionsManager _sessionsManager;
    private const int ServerVersion = 4;

    public RoomCreateRequestHandler(ILogger<RoomCreateRequestHandler> logger, SessionsManager sessionsManager)
    {
        _logger = logger;
        _sessionsManager = sessionsManager;
    }

    public async ValueTask HandleAsync(PacketContext context, RoomCreationRequestPacket packet)
    {
        if (packet.Version != ServerVersion)
        {
            var reason = packet.Version < ServerVersion
                ? ClajConnectionCloseReason.ObsoleteClient
                : ClajConnectionCloseReason.OutdatedServer;
            
            await context.Connection.SendTcp(new RoomClosedPacket
            {
                Reason = reason
            });

            context.Connection.RequestClose(ArcNetDcReason.Error);
            return;
        }

        if (
            context.Connection.ParticipatesInRoomId is { } roomId
            && _sessionsManager.GetRoom(roomId) is { } existingRoom
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

        var room = _sessionsManager.CreateRoom(context.Connection, packet.RoomType);
        _logger.LogInformation("Created room {@Room} ({roomIdStr}) for host {@Host}", room, room.IdString, context.Connection);
        
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