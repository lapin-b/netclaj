namespace NetClajServer.Packets.Framework;

public class KeepAlivePacket: MindustryPacket
{
    public const sbyte Type = PacketType.Framework;
    public const byte Identifier = 2;

    public override sbyte GetPacketFamily() => Type;
    public override byte GetPacketIdentifier() => Identifier;
    public override void Deserialize(BinaryReader reader)
    {
        // no-op
    }

    public override void Serialize(BinaryWriter writer)
    {
        // no-op
    }
}