using NetClajServer.Datastructures;

namespace NetClajServer.Packets.Claj;

public class RoomLinkPacket: IMindustryPacket
{
    public int RoomId { get; set; }
    
    public const sbyte Type = PacketType.Claj;
    public const byte Identifier = 6;

    sbyte IMindustryPacket.GetPacketType() => Type;
    public byte GetPacketIdentifier() => Identifier;
    
    public void Deserialize(BinaryReader reader)
    {
        RoomId = reader.ReadInt32BigEndian();
    }

    public void Serialize(BinaryWriter writer)
    {
        writer.WriteInt32BigEndian(RoomId);
    }
}