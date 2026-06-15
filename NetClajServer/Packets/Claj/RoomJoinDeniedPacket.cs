using System.Buffers;
using NetClajServer.Claj.Handlers;
using NetClajServer.Packets.IO;

namespace NetClajServer.Packets.Claj;

public class RoomJoinDeniedPacket: MindustryPacket
{
    public long RoomId { get; set; }
    public RoomRejection Reason { get; set; }
    
    public const sbyte Type = PacketType.Claj;
    public const byte Identifier = 10;

    public override sbyte GetPacketFamily() => Type;
    public override byte GetPacketIdentifier() => Identifier;

    public override void Serialize(IBufferWriter<byte> writer)
    {
        writer.WriteIntegerBe(RoomId);
        writer.WriteIntegerBe((byte)Reason);
    }

    public override PacketResult TryDeserialize(ref PacketReader reader)
    {
        reader.WithPacketName(nameof(RoomJoinDeniedPacket));
        RoomId = reader.ReadRoomId(nameof(RoomId));
        Reason = (RoomRejection)reader.ReadByte(nameof(Reason)).Value;
        return reader.Result;
    }
}