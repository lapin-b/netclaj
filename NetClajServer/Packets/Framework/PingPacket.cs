using NetClajServer.Datastructures;

namespace NetClajServer.Packets.Framework;

public class PingPacket: IMindustryPacket
{
    public const sbyte Type = PacketType.Framework;
    public const byte Identifier = 0;

    sbyte IMindustryPacket.GetPacketType() => Type;
    public byte GetPacketIdentifier() => Identifier;
    public void Deserialize(BinaryReader reader)
    {
        Id = reader.ReadInt32BigEndian();
        IsReply = reader.ReadBoolean();
    }

    public void Serialize(BinaryWriter writer)
    {
        writer.WriteInt32BigEndian(Id);
        writer.Write(IsReply);
    }
    
    public int Id { get; set; }
    public bool IsReply { get; set; }
}