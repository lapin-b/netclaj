using NetClajServer.Packets.IO;

namespace NetClajServer.Packets.Claj;

public class RoomListRequestPacket: MindustryPacket, ISequenceDeserializable
{
    public const sbyte Type = PacketType.Claj;
    public const byte Identifier = 18;
    
    public override sbyte GetPacketFamily() => Type;

    public override byte GetPacketIdentifier() => Identifier;

    public override void Deserialize(BinaryReader reader)
    {
        
    }

    public override void Serialize(BinaryWriter writer)
    {
        
    }

    public PacketResult TryDeserialize(ref PacketReader reader)
    {
        return reader.Result;
    }
}