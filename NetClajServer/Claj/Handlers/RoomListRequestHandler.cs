using Microsoft.Extensions.Logging;
using NetClajServer.Claj.PacketHandling;
using NetClajServer.Packets.Claj;

namespace NetClajServer.Claj.Handlers;

public class RoomListRequestHandler: IPacketHandler<RoomListRequestPacket>
{
    private readonly ILogger<RoomListRequestHandler> _logger;

    public RoomListRequestHandler(ILogger<RoomListRequestHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(PacketContext context, RoomListRequestPacket packet)
    {
        var roomList = context.Server.Rooms
            .Values
            .Where(r => r.Configuration is { IsPublic: true, CanRequestHostState: true })
            .ToList();

        using var globalTimeoutToken = new CancellationTokenSource(5000);
        var roomListRequestTasks = roomList.Select(async r =>
        {
            try
            {
                await r.RequestRoomState(5, globalTimeoutToken.Token);
            }
            catch (OperationCanceledException)
            {
                // no-op
            }
            catch(Exception e)
            {
                _logger.LogError(e, "Couldn't query room state for {roomId}", r.Id);
            }
        }).ToArray();

        await Task.WhenAll(roomListRequestTasks);

        var replyPacket = new RoomListPacket
        {
            Rooms = roomList
        };

        await context.Connection.SendStreaming(replyPacket);
    }
}