namespace NetClajServer.Packets.Framework;

public class KeepAlivePacket: IMindustryPacket
{
    public const sbyte Type = PacketType.Framework;
    public const byte Identifier = 2;

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