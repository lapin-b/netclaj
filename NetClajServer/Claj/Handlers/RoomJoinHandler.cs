using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NetClajServer.Claj.PacketHandling;
using NetClajServer.Packets;
using NetClajServer.Packets.Claj;

namespace NetClajServer.Claj.Handlers;

public class RoomJoinHandler: IPacketHandler<RoomJoinPacket>, IPacketHandler<RoomJoinRequestPacket>
{
    private readonly ILogger<RoomJoinHandler> _logger;

    public RoomJoinHandler(ILogger<RoomJoinHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask HandleAsync(PacketContext context, RoomJoinPacket packet)
    {
        var isRequest = packet is RoomJoinRequestPacket;
        var validation = ValidateRequest(context, packet.RoomId, packet.WithPin, packet.Pin, packet.RoomType);
        
        if (validation != RoomRejection.Success)
        {
            if (isRequest)
            {
                await context.Connection.SendTcp(new RoomJoinDeniedPacket()
                {
                    RoomId = packet.RoomId,
                    Reason = validation
                });
            }
            
            context.Connection.RequestClose(ArcNetDcReason.Closed);
            return;
        }

        context.Server.Rooms.TryGetValue(packet.RoomId, out var roomToJoin);
        Debug.Assert(roomToJoin != null, nameof(roomToJoin) + " != null");

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
        
        if (isRequest)
        {
            await context.Connection.SendTcp(new RoomJoinAcceptedPacket()
            {
                RoomId = roomToJoin.Id
            });

            return;
        }
        
        _logger.LogInformation("{ConnectionId} joining room {roomId}", context.Connection.Id, roomToJoin.Id);
        
        // TryJoinRoom will fail if the room is being dismantled or is closed
        if (!await roomToJoin.TryJoinRoom(context.Connection))
        {
            _logger.LogWarning("{ConnectionId} tried to join a room being dismantled", context.Connection.Id);
            context.Connection.RequestClose(ArcNetDcReason.Error);
            return;
        }
        
        while (context.Connection.RawPacketsQueue.Reader.TryRead(out var pendingPacket))
        {
            _logger.LogDebug("Submitting packets from {ConnectionId} into the room", context.Connection.Id);
            await roomToJoin.HandlePacket(context, pendingPacket);
        }
    }

    public ValueTask HandleAsync(PacketContext context, RoomJoinRequestPacket packet) => 
        HandleAsync(context, packet.AsRoomJoinPacket);

    private RoomRejection ValidateRequest(PacketContext context, long roomId, bool reqWithPin, short? reqPin, string reqRoomType)
    {
        if (!context.Server.Rooms.TryGetValue(roomId, out var room))
        {
            return RoomRejection.NotFound;
        }
        
        // TODO: Ratelimit joins

        if (room.RoomType != reqRoomType)
        {
            return RoomRejection.Incompatible;
        }

        if (room.Configuration.IsProtectedByPin && !reqWithPin)
        {
            if (!reqWithPin)
            {
                return RoomRejection.PinRequired;
            }

            if (room.Configuration.Pin != reqPin)
            {
                return RoomRejection.InvalidPin;
            }
        }

        if (room.Configuration.MaxClients > 0 && room.PlayersCount >= room.Configuration.MaxClients)
        {
            return RoomRejection.RoomFull;
        }
        
        return RoomRejection.Success;
    }
}

public enum RoomRejection: byte
{
    Error,
    ServerFull,
    ServerClosing,
    NotFound,
    RoomFull,
    PinRequired,
    InvalidPin,
    Incompatible,
    
    Success = 255,
}