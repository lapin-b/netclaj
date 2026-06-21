using System.Buffers;
using PacketHandling.IO;

namespace PacketHandling.Claj;

public class ConnectionIdlingPacket: MindustryPacketWithConId
{
    public const byte Identifier = 3;

    public override byte GetPacketIdentifier() => Identifier;
    
    protected override void TryDeserializeInnerPayload(ref PacketReader reader)
    {
        // no-op
    }

    protected override void SerializeInnerPayload(IBufferWriter<byte> writer)
    {
        // no-op
    }
}