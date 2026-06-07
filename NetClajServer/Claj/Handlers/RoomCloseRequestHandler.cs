using Microsoft.Extensions.Logging;
using NetClajServer.Claj.PacketHandling;
using NetClajServer.Mindustry;
using NetClajServer.Packets.Claj;

namespace NetClajServer.Claj.Handlers;

public class RoomCloseRequestHandler: IPacketHandler<RoomClosureRequestPacket>
{
    private readonly ILogger<RoomCloseRequestHandler> _logger;
    private readonly SessionsManager _sessionsManager;

    public RoomCloseRequestHandler(ILogger<RoomCloseRequestHandler> logger, SessionsManager sessionsManager)
    {
        _logger = logger;
        _sessionsManager = sessionsManager;
    }

    public ValueTask HandleAsync(PacketContext context, RoomClosureRequestPacket packet)
    {
        if (_sessionsManager.CheckRoomExistenceAndOwnership(context.Connection) is not { } room)
            return ValueTask.CompletedTask;

        _logger.LogInformation("Closing room {roomId} because host closed it", room.Id);
        return new ValueTask(_sessionsManager.CloseRoom(room.Id));
    }
}