using System.Buffers;
using PacketHandling.IO;
using PacketHandling.Serialization;

namespace PacketHandling.Claj;

public class RoomLinkPacket: MindustryPacket
{
    public long RoomId { get; set; }
    
    public const sbyte Type = PacketType.Claj;
    public const byte Identifier = 11;

    public override sbyte GetPacketFamily() => Type;
    public override byte GetPacketIdentifier() => Identifier;

    public override void Serialize(IBufferWriter<byte> writer)
    {
        writer.WriteIntegerBe(RoomId);
    }

    public override PacketResult TryDeserialize(ref PacketReader reader)
    {
        RoomId = reader.ReadRoomId(nameof(RoomId));
        return reader.Result;
    }
}