namespace NetClajServer.Packets.Claj;

public class ConnectionClosedPacket: MindustryPacketWithConId
{
    public ArcNetDcReason Reason { get; set; }
    
    public const byte Identifier = 1;

    public override byte GetPacketIdentifier() => Identifier;

    protected override void DeserializeInnerPayload(BinaryReader reader)
    {
        Reason = (ArcNetDcReason)reader.ReadByte();
    }

    protected override void SerializeInnerPayload(BinaryWriter writer)
    {
        writer.Write((byte)Reason);
    }
}