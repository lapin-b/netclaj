using NetClajServer.Claj.Handlers;
using NetClajServer.Datastructures;
using NetClajServer.Packets.IO;

namespace NetClajServer.Packets.Claj;

public class RoomJoinDeniedPacket: MindustryPacket, ISequenceDeserializable
{
    public long RoomId { get; set; }
    public RoomRejection Reason { get; set; }
    
    public const sbyte Type = PacketType.Claj;
    public const byte Identifier = 10;

    public override sbyte GetPacketFamily() => Type;
    public override byte GetPacketIdentifier() => Identifier;

    public override void Deserialize(BinaryReader reader)
    {
        throw new NotSupportedException();
    }

    public override void Serialize(BinaryWriter writer)
    {
        writer.WriteInt64BigEndian(RoomId);
        writer.Write((byte)Reason);
    }

    public PacketResult TryDeserialize(ref PacketReader reader)
    {
        reader.WithPacketName(nameof(RoomJoinDeniedPacket));
        RoomId = reader.NeedRoomId(nameof(RoomId));
        Reason = (RoomRejection)reader.NeedByte(nameof(Reason)).Value;
        return reader.Result;
    }
}