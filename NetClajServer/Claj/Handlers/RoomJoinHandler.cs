using System.Net.Mime;
using Microsoft.Extensions.Logging;
using NetClajServer.Claj.PacketHandling;
using NetClajServer.Packets;
using NetClajServer.Packets.Claj;

namespace NetClajServer.Claj.Handlers;

public class RoomJoinHandler: IPacketHandler<RoomJoinPacket>
{
    private readonly ILogger<RoomJoinHandler> _logger;

    public RoomJoinHandler(ILogger<RoomJoinHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(PacketContext context, RoomJoinPacket packet)
    {
        // Does the room even exist ?
        if (!context.Server.Rooms.TryGetValue(packet.RoomId, out var roomToJoin))
        {
            _logger.LogWarning(
                "Connection {connectionId} tried to join a non-existing room ID {roomId}",
                context.Connection.Id,
                packet.RoomId
            );
            
            await context.Connection.CloseAsync(ArcNetDcReason.Error);
            return;
        }

        // A player can leave a room freely (TryLeaveRoom will return true), but
        // the host can't.
        if (
            context.Server.FindConnectionInRooms(context.Connection) is {} alreadyJoinedRoom
            && !await alreadyJoinedRoom.TryLeaveRoom(context.Connection, true)
        )
        {
            _logger.LogWarning(
                "{Connection} tried to join room {targetRoomId}, but it is already hosting {hostingRoomId}",
                context.Connection.Id,
                roomToJoin.Id,
                alreadyJoinedRoom.Id
            );

            return;
        }

        _logger.LogInformation("{ConnectionId} joining room {roomId}", context.Connection.Id, roomToJoin.Id);
        
        // TryJoinRoom will fail if the room is being dismantled or is closed
        if (!await roomToJoin.TryJoinRoom(context.Connection))
        {
            _logger.LogWarning("{ConnectionId} tried to join a room being dismantled", context.Connection.Id);
            await context.Connection.CloseAsync(ArcNetDcReason.Error);
            return;
        }
        
        while (context.Connection.RawPacketsQueue.Reader.TryRead(out var pendingPacket))
        {
            _logger.LogDebug("Submitting packets from {ConnectionId} into the room", context.Connection.Id);
            await roomToJoin.HandlePacket(context, pendingPacket);
        }
    }
}