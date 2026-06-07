using System.Buffers;
using Microsoft.Extensions.Logging;
using NetClajServer.Claj.PacketHandling;
using NetClajServer.Mindustry;
using NetClajServer.Packets.Claj;

namespace NetClajServer.Claj.Handlers;

public class RoomStateHandler: IPacketHandler<RoomStatePacket>
{
    private readonly ILogger<RoomStateHandler> _logger;
    private readonly SessionsManager _sessionsManager;

    public RoomStateHandler(ILogger<RoomStateHandler> logger, SessionsManager sessionsManager)
    {
        _logger = logger;
        _sessionsManager = sessionsManager;
    }

    public ValueTask HandleAsync(PacketContext context, RoomStatePacket packet)
    {
        if(_sessionsManager.CheckRoomExistenceAndOwnership(context.Connection) is not { } room)
            return ValueTask.CompletedTask;
        
        _logger.LogInformation("Setting room {roomId} state", room.Id);
        // Materialize the packet buffer into a byte-array because the room
        // will outlive the lifetime of the ReadOnlySequence<byte> in the packet.
        room?.State = packet.StateBuffer.ToArray();
        return ValueTask.CompletedTask;
    }
}