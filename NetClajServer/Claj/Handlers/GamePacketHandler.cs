using Microsoft.Extensions.Logging;
using NetClajServer.Claj.PacketHandling;
using NetClajServer.Packets;
using NetClajServer.Packets.Claj;
using NetClajServer.Packets.Framework;

namespace NetClajServer.Claj.Handlers;

public class GamePacketHandler: IPacketHandler<GamePacket>, IPacketHandler<ClajPayloadWrapping>
{
    private readonly ILogger<GamePacketHandler> _logger;

    public GamePacketHandler(ILogger<GamePacketHandler> logger)
    {
        _logger = logger;
    }

    public ValueTask HandleAsync(PacketContext context, GamePacket packet)
    {
        return HandleGamePacketOrClajWrapping(context, packet);
    }

    public ValueTask HandleAsync(PacketContext context, ClajPayloadWrapping packet)
    {
        return HandleGamePacketOrClajWrapping(context, packet);
    }

    private ValueTask HandleGamePacketOrClajWrapping(PacketContext context, MindustryPacket packet)
    {
        if (context.Connection.ParticipatesInRoomId is not { } participatesInRoomId)
        {
            _logger.LogWarning("Connection {@Connection} is not in a room and is sending raw packets. Dropping", context.Connection);
            return ValueTask.CompletedTask;
        }
        
        if (context.Sessions.GetRoom(participatesInRoomId) is { } room)
        {
            if (packet is ClajPayloadWrapping && context.Connection.Id != room.HostConnectionId)
            {
                _logger.LogWarning(
                    "Received a Claj wrapping packet from {@Connection}, not the room hoster {HostConnectionId}. Dropping",
                    context.Connection,
                    room.HostConnectionId
                );

                return ValueTask.CompletedTask;
            }

            return room.HandlePacket(context, packet);
        }

        // The room class owns ParticipatesInRoomId management.
        // This should NOT happen.
        
        _logger.LogError(
            "Connection {@Connection} says it's partaking in room {roomId} but it doesn't exist. This shouldn't happen", 
            context.Connection, 
            participatesInRoomId
        );

        // The only time a handler can edit the participating room id to reflect reality
        // is when it is pointing to a room that doesn't exist.
        context.Connection.ParticipatesInRoomId = null;

        return ValueTask.CompletedTask;
    }
}