using NetClajServer.Packets.IO;

namespace NetClajServer.Packets.Claj;

public class RoomClosureRequestPacket: MindustryPacket, ISequenceDeserializable
{
    public const sbyte Type = PacketType.Claj;
    public const byte Identifier = 5;

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