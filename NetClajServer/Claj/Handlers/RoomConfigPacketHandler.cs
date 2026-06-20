using Microsoft.Extensions.Logging;
using NetClajServer.Claj.PacketHandling;
using NetClajServer.Mindustry;
using NetClajServer.Packets;
using PacketHandling.Claj;

namespace NetClajServer.Claj.Handlers;

public class RoomConfigPacketHandler: IPacketHandler<RoomConfigPacket>
{
    private readonly ILogger<RoomConfigPacketHandler> _logger;
    private readonly SessionsManager _sessionsManager;

    public RoomConfigPacketHandler(ILogger<RoomConfigPacketHandler> logger, SessionsManager sessionsManager)
    {
        _logger = logger;
        _sessionsManager = sessionsManager;
    }

    public ValueTask HandleAsync(PacketContext context, RoomConfigPacket packet)
    {
        var room = _sessionsManager.CheckRoomExistenceAndOwnership(context.Connection);
        if (room == null) return ValueTask.CompletedTask;
        
        _logger.LogInformation(
            "Set config for {@Room}. isPublic={isPublic}; isProtected={isProtected} ({Pin}), canQueryHost={canQueryHost}, maxClients={maxClients}",
            room,
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