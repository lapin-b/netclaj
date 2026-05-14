using System.Buffers;
using System.Text;
using NetClajServer.Claj;
using NetClajServer.Datastructures;
using NetClajServer.Mindustry;
using NetClajServer.Packets.IO;

namespace NetClajServer.Packets.Claj;

public class RoomCreationRequestPacket: MindustryPacket, ISequenceDeserializable
{
    public int? Version { get; set; }
    public string? RoomType { get; set; }
    
    public const sbyte Type = PacketType.Claj;
    public const byte Identifier = 4;

    public override sbyte GetPacketFamily() => Type;
    public override byte GetPacketIdentifier() => Identifier;
    
    public override void Deserialize(BinaryReader reader)
    {
        // The old protocol version used a UTF string to encode the version. Now, the major protocol version is used
        // to check for compatibility. Said version is on the 2nd byte relative to the "packet contents"
        var utflen = reader.ReadUInt16BigEndian();
        
        if (utflen == 0 && reader.BaseStream.HasBytesRemaining())
        {
            // TODO: Do the same thing as the CLaJ protocol, accept up to 16 bytes of data
            Version = reader.ReadInt32BigEndian();
            RoomType = new string(reader.ReadChars(reader.ReadByte()));
        }
    }
    
    public PacketResult TryDeserialize(ref PacketReader reader)
    {
        const string packetName = nameof(RoomCreationRequestPacket);

        var res = reader.NeedShortBigEndian(packetName, "UTF length", out var utflen);
        if(res.IsFailure) return res;

        res = reader.Require(
            utflen != 0 || reader.Remaining == 0, packetName,"UTF length",
            PacketErrorCode.InvalidValue, "Old client version detected or no more bytes to process"
        );
        if (res.IsFailure) return res;

        res = reader.NeedIntBigEndian(packetName, nameof(Version), out var version);
        if (res.IsFailure) return res;

        res = reader.NeedByte(packetName, nameof(RoomType), out var strLen);
        if (res.IsFailure) return res;

        res = reader.Require(
            strLen > 16, packetName, nameof(RoomType), 
            PacketErrorCode.LimitExceeded, "Room type string length is zero or more than 16"
        );
        if (res.IsFailure) return res;

        res = reader.TryReadExact(packetName, nameof(RoomType), strLen, out var roomTypeBytes);
        if (res.IsFailure) return res;
        
        Version = version;
        RoomType = Encoding.ASCII.GetString(roomTypeBytes);

        return PacketResult.Ok();
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