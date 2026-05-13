using NetClajServer.Claj;
using NetClajServer.Datastructures;

namespace NetClajServer.Packets.Claj;

public class RoomCreationRequestPacket: MindustryPacket
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