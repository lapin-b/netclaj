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

        if (!reader.TryReadShortBigEndian(packetName, "UTF length", out var utflen, out var err))
            return err;
        
        if (utflen != 0 || reader.Remaining == 0) 
            return PacketResult.Err(
                PacketErrorCode.UnexpectedEof, packetName, "UTF length", 
                reader.Consumed, "Old client version detected or no more bytes to process");

        if (!reader.TryReadIntBigEndian(packetName, nameof(Version), out var version, out err)) return err;
        
        if (!reader.TryReadByte(packetName, nameof(RoomType), out var strLen, out err))
            return err with { Detail = "Room type string length is zero or unreadable" };

        if (!reader.TryReadExact(packetName, nameof(RoomType), strLen, out var roomTypeBytes, out err)) return err;
        
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