namespace NetClajServer.Packets.Framework;

public class DiscoverHostPacket: IMindustryPacket
{
    public const sbyte Type = PacketType.Framework;
    public const byte Identifier = 1;

    sbyte IMindustryPacket.GetPacketType() => Type;
    public byte GetPacketIdentifier() => Identifier;
    public void Deserialize(BinaryReader reader)
    {
        // no-op
    }

    public void Serialize(BinaryWriter writer)
    {
        // no-op
    }
}