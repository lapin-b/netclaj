using NetClajServer.Datastructures;

namespace NetClajServer.Packets.Claj;

public class ClajMessagePacket: IMindustryPacket
{
    public string Message { get; set; }
    
    public const sbyte Type = PacketType.Claj;
    public const byte Identifier = 8;

    sbyte IMindustryPacket.GetPacketType() => Type;
    public byte GetPacketIdentifier() => Identifier;
    
    public void Deserialize(BinaryReader reader)
    {
        Message = reader.ReadJavaUtf();
    }

    public void Serialize(BinaryWriter writer)
    {
        writer.WriteJavaUtf(Message);
    }
}