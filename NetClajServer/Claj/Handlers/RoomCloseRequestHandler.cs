using Microsoft.Extensions.Logging;
using NetClajServer.Claj.PacketHandling;
using NetClajServer.Packets.Claj;

namespace NetClajServer.Claj.Handlers;

public class RoomCloseRequestHandler: IPacketHandler<RoomClosureRequestPacket>
{
    private readonly ILogger<RoomCloseRequestHandler> _logger;

    public RoomCloseRequestHandler(ILogger<RoomCloseRequestHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(PacketContext context, RoomClosureRequestPacket packet)
    {
        if (HandlerUtils.CheckRoomExistenceAndOwnership(context, _logger) is not { } room)
            return;

        _logger.LogInformation("Closing room {roomId} because host closed it", room.Id);
        await room.Close();
        context.Server.Rooms.TryRemove(room.Id, out _);
    }
}