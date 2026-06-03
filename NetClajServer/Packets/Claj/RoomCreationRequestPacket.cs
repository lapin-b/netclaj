using System.Text;
using NetClajServer.Datastructures;
using NetClajServer.Packets.IO;

namespace NetClajServer.Packets.Claj;

public class RoomCreationRequestPacket: MindustryPacket
{
    public int? Version { get; set; }
    public string RoomType { get; set; } = string.Empty;
    
    public const sbyte Type = PacketType.Claj;
    public const byte Identifier = 4;

    public override sbyte GetPacketFamily() => Type;
    public override byte GetPacketIdentifier() => Identifier;
    
    public override PacketResult TryDeserialize(ref PacketReader reader)
    {
        reader.WithPacketName(nameof(RoomCreationRequestPacket));

        // The old protocol version used a UTF string to encode the version. Now, the major protocol version is used
        // to check for compatibility. Said version is on the 2nd byte relative to the "packet contents"
        reader.ReadShortBigEndian("UTF length")
            .Ensure(l => l == 0, PacketErrorCode.InvalidValue, "UTF length is not zero");
        
        Version = reader.ReadIntBigEndian(nameof(Version));
        
        var strLen = reader
            .ReadByte(nameof(RoomType))
            .Ensure(l => l is > 0 and <= 16, PacketErrorCode.InvalidValue, "String length is zero length or more than 16");

        RoomType = reader
            .ReadExactBytes(nameof(RoomType), strLen)
            .Map(roomTypeBytes => Encoding.ASCII.GetString(roomTypeBytes));

        return reader.Result;
    }

    public override void Serialize(BinaryWriter writer)
    {
        if (Version == null)
        {
            throw new ArgumentNullException(nameof(Version));
        }

        ArgumentNullException.ThrowIfNull(RoomType);
        
        writer.Write((short)0);
        writer.WriteInt32BigEndian((int)Version);
        writer.Write(RoomType);
    }
}