using Microsoft.Extensions.Logging;
using NetClajServer.Claj.PacketHandling;
using NetClajServer.Packets.Claj;

namespace NetClajServer.Claj.Handlers;

public class RoomLeaveHandler: IPacketHandler<ConnectionClosedPacket>
{
    private readonly ILogger<RoomLeaveHandler> _logger;

    public RoomLeaveHandler(ILogger<RoomLeaveHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(PacketContext context, ConnectionClosedPacket packet)
    {
        if (context.Server.FindConnectionInRooms(context.Connection) is not {} room)
        {
            _logger.LogWarning("Tried to disconnect from a non-existing room");
            return;
        }

        if (context.Connection.Id != room.HostConnectionId)
        {
            _logger.LogWarning("Connection ID {ConnectionID} has tried to close another's player while not being the host", context.Connection.Id);
            return;
        }

        if (context.Server.Connections.TryGetValue(packet.ConnectionId, out var targetConnection))
        {
            _logger.LogInformation("Host made {ConnectionID} leave the room {RoomId}", targetConnection.Id, room.Id);
            await room.TryLeaveRoom(targetConnection);
        }
        else
        {
            _logger.LogWarning("Couldn't have {connectionId} leave room {roomId}: connection doesn't exist", packet.ConnectionId, room.Id);
        }
    }
}