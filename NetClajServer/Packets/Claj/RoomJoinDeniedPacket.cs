using NetClajServer.Claj.Handlers;

namespace NetClajServer.Packets.Claj;

public class RoomJoinDeniedPacket: MindustryPacket
{
    public RoomRejection Reason { get; set; }
    
    public const sbyte Type = PacketType.Claj;
    public const byte Identifier = 10;
    
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