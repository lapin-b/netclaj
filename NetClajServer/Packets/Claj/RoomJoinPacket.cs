namespace NetClajServer.Packets.Claj;

public class RoomJoinPacket: IMindustryPacket
{
    public const sbyte Type = PacketType.Claj;
    public const byte Identifier = 7;

    sbyte IMindustryPacket.GetPacketType() => Type;
    public byte GetPacketIdentifier() => Identifier;
    
    public void Deserialize(BinaryReader reader)
    {
        // no-op
    }

    public void Serialize(BinaryWriter writer)
    {
        //no-op
    }
}