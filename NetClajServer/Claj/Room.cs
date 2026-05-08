using System.Buffers.Binary;
using System.Buffers.Text;
using System.Collections.Concurrent;
using NetClajServer.Mindustry;
using NetClajServer.Packets.Claj;

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

    public Room(long roomId, Connection host)
    {
        Id = roomId;
        _host = host;
    }

    public async Task JoinRoom(Connection player)
    {
        _players.TryAdd(player.Id, player);
        
        await _host.SendTcp(new ConnectionJoinPacket
        {
            ConnectionId = player.Id
        });
    }

    public Task LeaveRoom(Connection player, bool keepOpen = false)
    {
        _players.TryRemove(player.Id, out _);
        if (!keepOpen)
        {
            player.Close();
        }
        
        return Task.CompletedTask;
    }

    public Task CloseRoom()
    {
        foreach (var player in _players.Values)
        {
            player.Close();
        }
        
        _players.Clear();

        return Task.CompletedTask;
    }

    public bool HasPlayer(Connection queryConnection) => _players.ContainsKey(queryConnection.Id);
    public bool HasPlayer(int connectionId) => _players.ContainsKey(connectionId);
}