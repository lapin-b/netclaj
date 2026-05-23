namespace NetClajServer.Packets.Claj;

public class RoomStateRequestPacket: MindustryPacket
{
    public const sbyte Type = PacketType.Claj;
    public const byte Identifier = 13;
    
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
}