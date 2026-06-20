using System.Buffers.Binary;
using System.Buffers.Text;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using NetClajServer.Claj.PacketHandling;
using NetClajServer.Mindustry;
using PacketHandling;
using PacketHandling.Claj;
using PacketHandling.Framework;

namespace NetClajServer.Claj;

public partial class Room
{
    public long Id { get; }

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
    public RoomConfiguration Configuration { get; set; }

    public byte[] State
    {
        get;
        set
        {
            field = value;
            _stateLastReceivedAt = DateTime.Now;
            Volatile.Read(ref _stateResponseReceived)?.TrySetResult();
        }
    } = [];
    public string RoomType { get; private init; }
    public int PlayersCount => _players.Count;
    
    private readonly Connection _host;
    private readonly ILogger<Room> _logger;
    private readonly ConcurrentDictionary<int, Connection> _players = new();
    
    // State management
    private int _closingStarted = 0;
    private readonly TaskCompletionSource _roomClosedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    
    // Relay querying state from the room host
    private TaskCompletionSource? _stateResponseReceived;
    private DateTime? _stateLastReceivedAt;
    
    public Room(long roomId, Connection host, string roomType, ILogger<Room> logger)
    {
        Id = roomId;
        _host = host;
        _logger = logger;
        Configuration = new RoomConfiguration();
        RoomType = roomType;

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

    public async Task RequestRoomState(int requestTimeoutSeconds, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (DateTime.Now - _stateLastReceivedAt < Constants.RoomStateFreshnessDuration)
        {
            return;
        }

        var existing = Volatile.Read(ref _stateResponseReceived);
        // A room state request is already pending
        if (existing is not null)
        {
            await existing.Task.WaitAsync(ct);
            return;
        }

        var createdTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var previousTcs = Interlocked.CompareExchange(ref _stateResponseReceived, createdTcs, null);

        if (previousTcs != null)
        {
            // We tried to set the completion task source to our own,
            // it was already set by another task now doing the request
            await previousTcs.Task.WaitAsync(ct);
            return;
        }

        try
        {
            await _host.SendTcp(new RoomStateRequestPacket());
            
            // This bit is the "timeout" part of requesting the room state to the client. If the request duration elapses,
            // an OperationCanceledException is thrown and caught in the catch before being rethrown.
            using var timeoutTokenSource = new CancellationTokenSource(new TimeSpan(0, 0, requestTimeoutSeconds));
            using var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutTokenSource.Token);
            await createdTcs.Task.WaitAsync(linkedToken.Token);
        }
        catch (Exception e)
        {
            createdTcs.TrySetException(e);
            throw;
        }
        finally
        {
            // Remove the task completion source if it's our own.
            Interlocked.CompareExchange(ref _stateResponseReceived, null, createdTcs);
        }
    }
    
    public ValueTask HandlePacket(PacketContext context, MindustryPacket mindustryPacket)
    {
        // Room host -> specific client
        if (context.Connection.Id == HostConnectionId)
        {
            // The host will only see Claj wrapping packets
            if (mindustryPacket is not ClajPayloadWrapping clajWrapper) 
                return ValueTask.CompletedTask;

            if (_players.TryGetValue(clajWrapper.ConnectionId, out var targetConnection) && targetConnection.IsConnected)
            {
                // The host tells us if the packet it sent should be relayed over TCP or UDP
                LogHostToClientPayloadRelay(Id, HostConnectionId, targetConnection.Id);
                var bufferToSend = new GamePacket
                {
                    Buffer = clajWrapper.Buffer
                };

                return clajWrapper.WrappedPacketIsTcp 
                    ? targetConnection.SendTcp(bufferToSend) 
                    : targetConnection.SendUdp(bufferToSend);
            }

            // Somehow this connection didn't exist, yet it still "participates" in this room for the host
            _logger.LogWarning(
                "Room {Id}: connection {targetId} doesn't exist or is disconnected, yet is still partaking in the room for the host.",
                Id,
                clajWrapper.ConnectionId
            );

            return _host.SendTcp(new ConnectionClosedPacket
            {
                ConnectionId = clajWrapper.ConnectionId,
                Reason = ArcNetDcReason.Error
            });
        }
        
        // Specific client -> room host
        if (_host.IsConnected && _players.ContainsKey(context.Connection.Id))
        {
            // Players never see a Claj packet, they only manipulate raw packets
            if (mindustryPacket is not GamePacket raw)
            {
                return ValueTask.CompletedTask;
            }
            
            var clajWrappedGamePacket = new ClajPayloadWrapping
            {
                ConnectionId = context.Connection.Id,
                Buffer = raw.Buffer,
                WrappedPacketIsTcp = raw.TransportIsTcp
            };
            
            LogClientToHostPayloadRelay(Id, context.Connection.Id, HostConnectionId);

            return _host.SendTcp(clajWrappedGamePacket);
        }

        return ValueTask.CompletedTask;
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
                _roomClosedTcs.TrySetResult();
            }
        }
        else
        {
            await _roomClosedTcs.Task;
        }
    }

    public static RoomListItem IntoRoomListItem(Room room) => new(room.Id, room.Configuration.IsProtectedByPin, room.State);
}