using NetClajServer.Packets.IO;

namespace NetClajServer.Packets.Claj;

public class RoomClosureRequestPacket: MindustryPacket
{
    public const sbyte Type = PacketType.Claj;
    public const byte Identifier = 5;

    public override sbyte GetPacketFamily() => Type;
    public override byte GetPacketIdentifier() => Identifier;

    public override void Serialize(BinaryWriter writer)
    {
        // no-op
    }

    public override PacketResult TryDeserialize(ref PacketReader reader)
    {
        // no-op
        return reader.Result;
    }
}