using NetClajServer.Datastructures;

namespace NetClajServer.Packets.Claj;

public class RoomLinkPacket: IMindustryPacket
{
    public long RoomId { get; set; }
    
    public const sbyte Type = PacketType.Claj;
    public const byte Identifier = 6;

    sbyte IMindustryPacket.GetPacketType() => Type;
    public byte GetPacketIdentifier() => Identifier;
    
    public void Deserialize(BinaryReader reader)
    {
        RoomId = reader.ReadInt64BigEndian();
    }

    public void Serialize(BinaryWriter writer)
    {
        writer.WriteInt64BigEndian(RoomId);
    }
}