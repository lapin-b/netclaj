using NetClajServer.Packets.IO;

namespace NetClajServer.Packets.Claj;

public class RoomInfoPacket: MindustryPacket
{
    public const sbyte Type = PacketType.Claj;
    public const byte Identifier = 17;
    
    public override sbyte GetPacketFamily() => Type;

    public override byte GetPacketIdentifier() => Identifier;

    public override void Serialize(BinaryWriter writer)
    {
        throw new NotImplementedException();
    }

    public override PacketResult TryDeserialize(ref PacketReader reader)
    {
        throw new NotImplementedException();
    }
}