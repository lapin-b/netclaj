using System.Buffers.Binary;
using System.Buffers.Text;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using NetClajServer.Claj.PacketHandling;
using NetClajServer.Mindustry;
using NetClajServer.Packets;
using NetClajServer.Packets.Claj;
using NetClajServer.Packets.Framework;

namespace NetClajServer.Claj;

public class Room
{
    public long Id { get; set; }
    public string IdString {
        get
        {
            Span<byte> longBytes = stackalloc byte[8];
            BinaryPrimitives.WriteInt64BigEndian(longBytes, Id);
            return Base64Url.EncodeToString(longBytes);
        }
    }
    public int HostConnectionId => _host.Id;
    public bool IsClosed => Volatile.Read(ref _closingStarted) == 1;
    
    private readonly Connection _host;
    private readonly ILogger<Room> _logger;
    private readonly ConcurrentDictionary<int, Connection> _players = new();
    
    // State management
    private int _closingStarted = 0;
    private readonly TaskCompletionSource _closedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    
    public Room(long roomId, Connection host, ILogger<Room> logger)
    {
        Id = roomId;
        _host = host;
        _logger = logger;

        host.ParticipatesInRoomId = Id;
    }
    
    public ValueTask Close() => new(ExecuteRoomTeardown()); 
    
    public async Task<bool> TryJoinRoom(Connection player)
    {
        if (IsClosed)
        {
            return false;
        }
        
        _players.TryAdd(player.Id, player);
        player.ParticipatesInRoomId = Id;

        await _host.SendTcp(new ConnectionJoinPacket
        {
            ConnectionId = player.Id,
            RoomId = Id
        });

        return true;
    }

    public async Task<bool> TryLeaveRoom(Connection player, bool keepOpen = false, bool notifyHost = false)
    {
        if (player.Id == HostConnectionId)
        {
            // A host cannot leave its own room unless it closes it
            return false;
        }
        
        _players.TryRemove(player.Id, out _);
        player.ParticipatesInRoomId = null;
        if (!keepOpen)
        {
            player.RequestClose(ArcNetDcReason.Closed);
        }

        if (notifyHost)
        {
            await _host.SendTcp(new ConnectionClosedPacket
            {
                ConnectionId = player.Id, 
                Reason = ArcNetDcReason.Closed
            });
        }

        return true;
    }

    public bool HasPlayer(Connection queryConnection) => _players.ContainsKey(queryConnection.Id);
    public bool HasPlayer(int connectionId) => _players.ContainsKey(connectionId);

    public async Task HandlePacket(PacketContext context, MindustryPacket mindustryPacket)
    {
        // Room host -> specific client
        if (context.Connection.Id == HostConnectionId)
        {
            // The host will only see Claj wrapping packets
            if (mindustryPacket is not ClajPayloadWrapping clajWrapper)
            {
                return;
            }

            if (_players.TryGetValue(clajWrapper.ConnectionId, out var targetConnection) && targetConnection.IsConnected)
            {
                // The hosts tells us if the packet it sent should be relayed over TCP or UDP
                _logger.LogDebug("{RoomId} H -> P: {HostId} relaying to {targetId} {payload}", Id, HostConnectionId, targetConnection.Id, clajWrapper.Buffer);
                var bufferToSend = new GamePacket(clajWrapper.Buffer);
                await targetConnection.Send(bufferToSend, clajWrapper.WrappedPacketIsTcp);
                await _host.SendTcp(new ConnectionIdlingPacket { ConnectionId = targetConnection.Id });
            }
            else
            {
                // Somehow this connection didn't exist, yet it still "participates" in this room for the host
                _logger.LogWarning(
                    "Room {Id}: connection {targetId} doesn't exist or is disconnected, yet is still partaking in the room for the host.",
                    Id,
                    clajWrapper.ConnectionId
                );

                await _host.SendTcp(new ConnectionClosedPacket
                {
                    ConnectionId = clajWrapper.ConnectionId,
                    Reason = ArcNetDcReason.Error
                });
            }
        }
        // Specific client -> room host
        else if (_host.IsConnected && _players.ContainsKey(context.Connection.Id))
        {
            // Players never see a Claj packet, they only manipulate raw packets
            if (mindustryPacket is not GamePacket raw)
            {
                return;
            }
            
            var clajWrapped = new ClajPayloadWrapping()
            {
                ConnectionId = context.Connection.Id,
                // TODO: use a ReadOnlyMemory
                Buffer = raw.Buffer.ToArray(),
                WrappedPacketIsTcp = context.IsTcp
            };
            
            _logger.LogDebug("P -> H {RoomId}: {sourceId} relaying to {hostId} {payload}", Id, context.Connection.Id, HostConnectionId, Serializer.Serialize((clajWrapped)));

            await _host.SendTcp(clajWrapped);
        }
    }

    private async Task ExecuteRoomTeardown()
    {
        if (Interlocked.Exchange(ref _closingStarted, 1) == 0)
        {
            // First caller does the room shutdown work
            try
            {
                /*
                 * Since the host is no longer interested in the room and will close its connection soon,
                 * we can get away with closing the player's connections without telling the host.
                 */
                foreach (var player in _players.Values)
                {
                    await _host.SendTcp(new ConnectionClosedPacket() { ConnectionId = player.Id });
                    player.ParticipatesInRoomId = null;
                    player.RequestClose(ArcNetDcReason.Closed);
                }
            }
            finally
            {
                _host.ParticipatesInRoomId = null;
                _players.Clear();
                _closedTcs.TrySetResult();
            }
        }
        else
        {
            await _closedTcs.Task;
        }
    }
}