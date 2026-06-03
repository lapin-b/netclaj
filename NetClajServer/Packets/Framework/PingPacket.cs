using NetClajServer.Datastructures;
using NetClajServer.Packets.IO;

namespace NetClajServer.Packets.Framework;

public class PingPacket: MindustryPacket, ISequenceDeserializable
{
    public const sbyte Type = PacketType.Framework;
    public const byte Identifier = 0;

    public int Id { get; set; }
    public bool IsReply { get; set; }
    
    public override sbyte GetPacketFamily() => Type;
    public override byte GetPacketIdentifier() => Identifier;
    public override void Deserialize(BinaryReader reader)
    {
        Id = reader.ReadInt32BigEndian();
        IsReply = reader.ReadBoolean();
    }
    
    public PacketResult TryDeserialize(ref PacketReader reader)
    {
        reader.WithPacketName(nameof(PingPacket));
        
        Id = reader.NeedIntBigEndian(nameof(Id));
        IsReply = reader.NeedBoolean(nameof(IsReply));

        return reader.Result;
    }

    public override void Serialize(BinaryWriter writer)
    {
        writer.WriteInt32BigEndian(Id);
        writer.Write(IsReply);
    }
}