using NetClajServer.Datastructures;

namespace NetClajServer.Packets.Claj;

public class RoomCreateRequestPacket: MindustryPacket
{
    public string Version { get; set; }
    
    public const sbyte Type = PacketType.Claj;
    public const byte Identifier = 4;

    public override sbyte GetPacketType() => Type;
    public override byte GetPacketIdentifier() => Identifier;
    
    public override void Deserialize(BinaryReader reader)
    {
        Version = reader.ReadJavaUtf();
    }

    public override void Serialize(BinaryWriter writer)
    {
        writer.WriteJavaUtf(Version);
    }
}