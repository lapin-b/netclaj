using NetClajServer.Datastructures;

namespace NetClajServer.Packets.Claj;

public class RoomCreateRequestPacket: IMindustryPacket
{
    public string Version { get; set; }
    
    public const sbyte Type = PacketType.Claj;
    public const byte Identifier = 4;

    sbyte IMindustryPacket.GetPacketType() => Type;
    public byte GetPacketIdentifier() => Identifier;
    
    public void Deserialize(BinaryReader reader)
    {
        Version = reader.ReadJavaUtf();
    }

    public void Serialize(BinaryWriter writer)
    {
        writer.WriteJavaUtf(Version);
    }
}