namespace NetClajServer.Packets.Claj;

public class ConnectionIdlingPacket: MindustryPacketWithConId
{
    public const byte Identifier = 3;

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