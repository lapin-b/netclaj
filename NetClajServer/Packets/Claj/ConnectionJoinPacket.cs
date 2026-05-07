namespace NetClajServer.Packets.Claj;

public class ConnectionJoinPacket: MindustryPacketWithConId
{
    public const byte Identifier = 2;

    public override byte GetPacketIdentifier() => Identifier;
    
    protected override void DeserializeInnerPayload(BinaryReader reader)
    {
        // no-op
    }

    protected override void SerializeInnerPayload(BinaryWriter writer)
    {
        // no-op
    }
}