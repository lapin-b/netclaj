using System.Buffers;
using PacketHandling.IO;
using PacketHandling.Serialization;

namespace PacketHandling.Claj;

public class RoomJoinAcceptedPacket: MindustryPacket
{
    public long RoomId { get; set; }
    
    private const sbyte Type = PacketType.Claj;
    public const byte Identifier = 9;
    
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