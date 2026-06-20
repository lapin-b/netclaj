using System.Buffers;
using NetClajServer.Packets.IO;

namespace NetClajServer.Packets.Claj;

public class RoomCreationRequestPacket: MindustryPacket
{
    public int? Version { get; set; }
    public ClajRoomType RoomType { get; set; } = ClajRoomType.Empty;
    
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
        RoomType = ClajRoomType.FromPacketReader(ref reader);

        return reader.Result;
    }

    public override void Serialize(IBufferWriter<byte> writer)
    {
        if (Version == null)
        {
            throw new ArgumentNullException(nameof(Version));
        }

        writer.WriteIntegerBe((short)0);
        writer.WriteInt32BigEndian((int)Version);
        RoomType.Serialize(writer);
    }
}