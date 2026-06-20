using Microsoft.Extensions.Logging;
using NetClajServer.Claj.PacketHandling;
using NetClajServer.Mindustry;
using PacketHandling.Claj;

namespace NetClajServer.Claj.Handlers;

public class RoomLeaveHandler: IPacketHandler<ConnectionClosedPacket>
{
    private readonly ILogger<RoomLeaveHandler> _logger;
    private readonly SessionsManager _sessionsManager;

    public RoomLeaveHandler(ILogger<RoomLeaveHandler> logger, SessionsManager sessionsManager)
    {
        _logger = logger;
        _sessionsManager = sessionsManager;
    }

    public async ValueTask HandleAsync(PacketContext context, ConnectionClosedPacket packet)
    {
        if (_sessionsManager.FindConnectionInRooms(context.Connection) is not {} room)
        {
            _logger.LogWarning("Tried to disconnect from a non-existing room");
            return;
        }

        if (context.Connection.Id != room.HostConnectionId)
        {
            _logger.LogWarning("Connection ID {ConnectionID} has tried to close another's player while not being the host", context.Connection.Id);
            return;
        }

        if (_sessionsManager.GetConnectionById(packet.ConnectionId) is {} targetConnection)
        {
            _logger.LogInformation("Host made {@Connection} leave the room {@Room}", targetConnection, room);
            await room.TryLeaveRoom(targetConnection);
        }
        else
        {
            _logger.LogWarning("Couldn't have {connectionId} leave room {roomId}: connection doesn't exist", packet.ConnectionId, room.Id);
        }
    }
}