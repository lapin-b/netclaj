using System.Buffers;
using Microsoft.Extensions.Logging;
using NetClajServer.Claj.PacketHandling;
using NetClajServer.Packets.Claj;

namespace NetClajServer.Claj.Handlers;

public class RoomStateHandler: IPacketHandler<RoomStatePacket>
{
    private readonly ILogger<RoomStateHandler> _logger;

    public RoomStateHandler(ILogger<RoomStateHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(PacketContext context, RoomStatePacket packet)
    {
        if(HandlerUtils.CheckRoomExistenceAndOwnership(context, _logger) is not { } room)
            return Task.CompletedTask;
        
        _logger.LogInformation("Setting room {roomId} state", room.Id);
        // Materialize the packet buffer into a byte-array because the room
        // will outlive the lifetime of the ReadOnlySequence<byte> in the packet.
        room?.State = packet.StateBuffer.ToArray();
        return Task.CompletedTask;
    }
}