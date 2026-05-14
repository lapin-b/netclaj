using System.Buffers;
using NetClajServer.Datastructures;

namespace NetClajServer.Packets.Framework;

public class PingPacket: MindustryPacket, ISequenceDeserializable
{
    public const sbyte Type = PacketType.Framework;
    public const byte Identifier = 0;

    public override sbyte GetPacketFamily() => Type;
    public override byte GetPacketIdentifier() => Identifier;
    public override void Deserialize(BinaryReader reader)
    {
        Id = reader.ReadInt32BigEndian();
        IsReply = reader.ReadBoolean();
    }
    
    public void Deserialize(ref SequenceReader<byte> reader)
    {
        throw new NotImplementedException();
    }

    public override void Serialize(BinaryWriter writer)
    {
        writer.WriteInt32BigEndian(Id);
        writer.Write(IsReply);
    }
    
    public int Id { get; set; }
    public bool IsReply { get; set; }

}