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
            var longBytes = new byte[8];
            BinaryPrimitives.WriteInt64BigEndian(longBytes, Id);
            return Base64Url.EncodeToString(longBytes);
        }
    }

    public int HostConnectionId => _host.Id;
    
    private readonly Connection _host;
    private readonly ConcurrentDictionary<int, Connection> _players = new();
    private bool _closed = false;

    public Room(long roomId, Connection host)
    {
        Id = roomId;
        _host = host;
    }

    public async Task JoinRoom(Connection player)
    {
        if (_closed) return;
        _players.TryAdd(player.Id, player);

        await _host.SendTcp(new ConnectionJoinPacket
        {
            ConnectionId = player.Id,
            RoomId = Id
        });
    }

    public Task LeaveRoom(Connection player, bool keepOpen = false)
    {
        if (_closed) return Task.CompletedTask;

        _players.TryRemove(player.Id, out _);
        if (!keepOpen)
        {
            player.Close();
        }
        
        return Task.CompletedTask;
    }

    public Task CloseRoom()
    {
        if (Interlocked.Exchange(ref _closed, true)) return Task.CompletedTask;

        foreach (var player in _players.Values)
        {
            player.Close();
        }
        
        _players.Clear();
        _host.ParticipatesInRoomId = null;

        return Task.CompletedTask;
    }

    public bool HasPlayer(Connection queryConnection) => _players.ContainsKey(queryConnection.Id);
    public bool HasPlayer(int connectionId) => _players.ContainsKey(connectionId);

    public async Task HandlePacket(PacketContext context, MindustryPacket mindustryPacket)
    {
        if (_closed) return;

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
                context.Logger.LogDebug("{RoomId} H -> P: {HostId} relaying to {targetId}", Id, HostConnectionId, targetConnection.Id);
                var bufferToSend = new RawPacket(clajWrapper.Buffer);
                await targetConnection.Send(bufferToSend, clajWrapper.IsTcp);
            }
            else
            {
                // Somehow this connection didn't exist, yet it still "participates" in this room for the host
                context.Logger.LogWarning(
                    "Room {Id}: connection {targetId} doesn't exist or is disconnected, yet is still partaking in the room for the host.",
                    Id,
                    clajWrapper.ConnectionId
                );

                await _host.SendTcp(new ConnectionClosedPacket
                {
                    ConnectionId = clajWrapper.ConnectionId,
                    Reason = ConnectionCloseReason.Error
                });
            }
        }
        // Specific client -> room host
        else if (_host.IsConnected && _players.ContainsKey(context.Connection.Id))
        {
            // Players never see a Claj packet, they only manipulate raw packets
            if (mindustryPacket is not RawPacket raw)
            {
                return;
            }
            
            context.Logger.LogDebug("P -> H {RoomId}: {sourceId} relaying to {hostId}", Id, context.Connection.Id, HostConnectionId);
            var clajWrapped = new ClajPayloadWrapping()
            {
                ConnectionId = context.Connection.Id,
                Buffer = raw.Buffer,
                IsTcp = context.IsTcp
            };

            await _host.SendTcp(clajWrapped);
        }
    }
}