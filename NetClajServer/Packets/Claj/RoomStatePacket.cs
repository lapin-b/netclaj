namespace NetClajServer.Packets.Claj;

public class RoomStatePacket: MindustryPacket
{
    public const sbyte Type = PacketType.Claj;
    public const byte Identifier = 14;
    
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
}