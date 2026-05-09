using NetClajServer.Datastructures;

namespace NetClajServer.Packets.Claj;

public class ClajMessagePacket: MindustryPacket
{
    public string Message { get; set; }
    
    public const sbyte Type = PacketType.Claj;
    public const byte Identifier = 8;

    public override sbyte GetPacketType() => Type;
    public override byte GetPacketIdentifier() => Identifier;
    
    public override void Deserialize(BinaryReader reader)
    {
        Message = reader.ReadJavaUtf();
    }

    public override void Serialize(BinaryWriter writer)
    {
        writer.WriteJavaUtf(Message);
    }
}