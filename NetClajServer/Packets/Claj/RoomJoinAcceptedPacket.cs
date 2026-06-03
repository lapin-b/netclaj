using NetClajServer.Datastructures;
using NetClajServer.Packets.IO;

namespace NetClajServer.Packets.Claj;

public class RoomJoinAcceptedPacket: MindustryPacket
{
    public long RoomId { get; set; }
    
    private const sbyte Type = PacketType.Claj;
    public const byte Identifier = 9;
    
    public override sbyte GetPacketFamily() => Type;
    public override byte GetPacketIdentifier() => Identifier;

    public override void Serialize(BinaryWriter writer)
    {
        writer.WriteInt64BigEndian(RoomId);
    }

    public override PacketResult TryDeserialize(ref PacketReader reader)
    {
        RoomId = reader.ReadRoomId(nameof(RoomId));
        return reader.Result;
    }
}