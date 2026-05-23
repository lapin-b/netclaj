using NetClajServer.Packets.IO;

namespace NetClajServer.Packets.Claj;

public class ConnectionClosedPacket: MindustryPacketWithConId
{
    public ArcNetDcReason Reason { get; set; }
    
    public const byte Identifier = 1;

    public override byte GetPacketIdentifier() => Identifier;

    protected override void TryDeserializeInnerPayload(ref PacketReader reader)
    {
        reader.NeedByte(nameof(ConnectionClosedPacket), nameof(Reason), out var reason);
        Reason = (ArcNetDcReason)reason;
    }

    protected override void SerializeInnerPayload(BinaryWriter writer)
    {
        writer.Write((byte)Reason);
    }
}