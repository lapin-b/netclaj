using NetClajServer.Datastructures;

namespace NetClajServer.Packets.Framework;

public class PingPacket: MindustryPacket
{
    public const sbyte Type = PacketType.Framework;
    public const byte Identifier = 0;

    public override sbyte GetPacketType() => Type;
    public override byte GetPacketIdentifier() => Identifier;
    public override void Deserialize(BinaryReader reader)
    {
        Id = reader.ReadInt32BigEndian();
        IsReply = reader.ReadBoolean();
    }

    public override void Serialize(BinaryWriter writer)
    {
        writer.WriteInt32BigEndian(Id);
        writer.Write(IsReply);
    }
    
    public int Id { get; set; }
    public bool IsReply { get; set; }
}