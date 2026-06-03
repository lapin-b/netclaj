using NetClajServer.Claj;
using NetClajServer.Datastructures;
using NetClajServer.Packets.IO;

namespace NetClajServer.Packets.Claj;

public class ClajMessagePacket: MindustryPacket, ISequenceDeserializable
{
    public const sbyte Type = PacketType.Claj;
    public const byte Identifier = 22;
    public ClajMessages Message { get; set; }

    public override sbyte GetPacketFamily() => Type;
    public override byte GetPacketIdentifier() => Identifier;
    
    public override void Deserialize(BinaryReader reader)
    {
        Message = (ClajMessages)reader.ReadInt32BigEndian();
    }

    public override void Serialize(BinaryWriter writer)
    {
        writer.WriteInt32BigEndian((int)Message);
    }

    public PacketResult TryDeserialize(ref PacketReader reader)
    {
        Message = (ClajMessages)reader.ReadIntBigEndian(nameof(Message)).Value;
        return reader.Result;
    }
}