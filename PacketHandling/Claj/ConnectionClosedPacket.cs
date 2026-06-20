using System.Buffers;
using PacketHandling.IO;

namespace PacketHandling.Claj;

public class ConnectionClosedPacket: MindustryPacketWithConId
{
    public ArcNetDcReason Reason { get; set; }
    
    public const byte Identifier = 1;

    public override byte GetPacketIdentifier() => Identifier;

    protected override void TryDeserializeInnerPayload(ref PacketReader reader)
    {
        reader.WithPacketName(nameof(ConnectionClosedPacket));
        Reason = (ArcNetDcReason)reader.ReadByte(nameof(Reason)).Value;
    }

    protected override void SerializeInnerPayload(IBufferWriter<byte> writer)
    {
        writer.WriteIntegerBe((byte)Reason);
    }
}