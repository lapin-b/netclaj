using NetClajServer.Packets.IO;

namespace NetClajServer.Packets.Framework;

public class DiscoverHostPacket : MindustryPacket, ISequenceDeserializable
{
    public const sbyte Type = PacketType.Framework;
    public const byte Identifier = 1;

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

    public PacketResult TryDeserialize(ref PacketReader reader)
    {
        return reader.Result;
    }
}