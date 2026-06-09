using Microsoft.Extensions.Logging;
using NetClajServer.Claj.PacketHandling;
using NetClajServer.Mindustry;
using NetClajServer.Packets.Claj;

namespace NetClajServer.Claj.Handlers;

public class RoomListRequestHandler: IPacketHandler<RoomListRequestPacket>
{
    private readonly ILogger<RoomListRequestHandler> _logger;
    private readonly SessionsManager _sessionsManager;

    public RoomListRequestHandler(ILogger<RoomListRequestHandler> logger, SessionsManager sessionsManager)
    {
        _logger = logger;
        _sessionsManager = sessionsManager;
    }

    public async ValueTask HandleAsync(PacketContext context, RoomListRequestPacket packet)
    {
        var roomList = _sessionsManager
            .Rooms
            .Values
            .Where(r => r.RoomType == packet.RequestedRoomType && r.Configuration is { IsPublic: true, CanRequestHostState: true })
            .ToList();

        using var globalTimeoutToken = new CancellationTokenSource(Constants.RoomStateQueryGlobalTimeout);
        var roomListRequestTasks = roomList.Select(async r =>
        {
            try
            {
                await r.RequestRoomState(Constants.RoomStateQueryTimeout, globalTimeoutToken.Token);
            }
            catch (OperationCanceledException)
            {
                // no-op
            }
            catch(Exception e)
            {
                _logger.LogError(e, "Couldn't query room state for {@room}", r);
            }
        });

        try
        {
            await Task.WhenAll(roomListRequestTasks);
        }
        catch (OperationCanceledException)
        {
            // no-op
        }

        var replyPacket = new RoomListPacket
        {
            Rooms = roomList
        };

        await context.Connection.SendStreaming(replyPacket);
    }
}