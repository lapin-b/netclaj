using NetClajServer.Datastructures;

namespace NetClajServer.Packets.Claj;

public class RoomJoinPacket: MindustryPacket
{
    public long RoomId { get; set; }
    
    public const sbyte Type = PacketType.Claj;
    public const byte Identifier = 7;

    public override sbyte GetPacketType() => Type;
    public override byte GetPacketIdentifier() => Identifier;
    
    public override void Deserialize(BinaryReader reader)
    {
        RoomId = reader.ReadInt64BigEndian();
    }

    public override void Serialize(BinaryWriter writer)
    {
        writer.WriteInt64BigEndian(RoomId);
    }
}