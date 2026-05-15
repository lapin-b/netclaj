using NetClajServer.Datastructures;

namespace NetClajServer.Packets.Claj;

public class RoomJoinAcceptedPacket: MindustryPacket
{
    public long RoomId { get; set; }
    
    private const sbyte Type = PacketType.Claj;
    public const byte Identifier = 9;
    
    public override sbyte GetPacketFamily() => Type;
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