using NetClajServer.Packets.IO;

namespace NetClajServer.Packets.Claj;

public class RoomInfoRequestPacket: MindustryPacket, ISequenceDeserializable
{
    public const sbyte Type = PacketType.Claj;
    public const byte Identifier = 15;
    
    public override sbyte GetPacketFamily() => Type;

    public override byte GetPacketIdentifier() => Identifier;

    public override void Deserialize(BinaryReader reader)
    {
        throw new NotImplementedException();
    }

    public override void Serialize(BinaryWriter writer)
    {
        throw new NotImplementedException();
    }

    public PacketResult TryDeserialize(ref PacketReader reader)
    {
        throw new NotImplementedException();
    }
}