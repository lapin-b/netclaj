using NetClajServer.Datastructures;

namespace NetClajServer.Packets.Claj;

public class ClajPopupPacket: MindustryPacket
{
    public string Message { get; set; }
    
    public const sbyte Type = PacketType.Claj;
    public const byte Identifier = 23;

    public override sbyte GetPacketFamily() => Type;
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