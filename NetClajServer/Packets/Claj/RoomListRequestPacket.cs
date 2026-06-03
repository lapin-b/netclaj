using NetClajServer.Packets.IO;

namespace NetClajServer.Packets.Claj;

public class RoomListRequestPacket: MindustryPacket
{
    public const sbyte Type = PacketType.Claj;
    public const byte Identifier = 18;
    
    public override sbyte GetPacketFamily() => Type;

    public override byte GetPacketIdentifier() => Identifier;

    public override void Serialize(BinaryWriter writer)
    {
        
    }

    public override PacketResult TryDeserialize(ref PacketReader reader)
    {
        return reader.Result;
    }
}