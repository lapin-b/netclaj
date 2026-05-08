using System.Buffers.Binary;
using System.Buffers.Text;
using NetClajServer.Mindustry;

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
    private readonly List<int> _players = new();

    public Room(long roomId, Connection host)
    {
        Id = roomId;
        _host = host;
    }
}