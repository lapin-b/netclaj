using Microsoft.Extensions.Logging;
using NetClajServer.Claj.PacketHandling;
using NetClajServer.Packets.Claj;

namespace NetClajServer.Claj.Handlers;

public class RoomCloseRequestHandler: IPacketHandler<RoomCloseRequestPacket>
{
    private readonly ILogger<RoomCloseRequestHandler> _logger;

    public RoomCloseRequestHandler(ILogger<RoomCloseRequestHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(PacketContext context, RoomCloseRequestPacket packet)
    {
        if (context.Server.FindConnectionInRooms(context.Connection) is not {} room)
        {
            _logger.LogWarning("Connection {ConnectionID} is not bound to any room", context.Connection.Id);
            return;
        }

        if (context.Connection.Id != room.HostConnectionId)
        {
            _logger.LogWarning(
                "Connection {ConnectionID} tried to close a room it didn't host ({hostConnectionId} does)", 
                context.Connection.Id, 
                room.HostConnectionId
            );
            return;
        }

        _logger.LogInformation("Closing room {roomId} because host closed it", room.Id);
        await room.Close();
        context.Server.Rooms.TryRemove(room.Id, out _);
    }
}