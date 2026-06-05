using Microsoft.Extensions.Logging;
using NetClajServer.Claj.PacketHandling;
using NetClajServer.Packets.Claj;

namespace NetClajServer.Claj.Handlers;

public class RoomConfigPacketHandler: IPacketHandler<RoomConfigPacket>
{
    private readonly ILogger<RoomConfigPacketHandler> _logger;

    public RoomConfigPacketHandler(ILogger<RoomConfigPacketHandler> logger)
    {
        _logger = logger;
    }

    public ValueTask HandleAsync(PacketContext context, RoomConfigPacket packet)
    {
        var room = HandlerUtils.CheckRoomExistenceAndOwnership(context, _logger);
        if (room == null) return ValueTask.CompletedTask;
        
        _logger.LogInformation(
            "Set config for {RoomId}. isPublic={isPublic}; isProtected={isProtected} ({Pin}), canQueryHost={canQueryHost}, maxClients={maxClients}",
            context.Connection.ParticipatesInRoomId,
            packet.IsPublic,
            packet.IsProtectedByPin,
            packet.Pin,
            packet.CanRequestHostState,
            packet.MaxClients
        );

        room.Configuration = packet.IntoRoomConfiguration();
        
        return ValueTask.CompletedTask;
    }
}