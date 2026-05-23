using NetClajServer.Packets.IO;

namespace NetClajServer.Packets.Claj;

public class ConnectionIdlingPacket: MindustryPacketWithConId
{
    public const byte Identifier = 3;

    public override byte GetPacketIdentifier() => Identifier;
    
    protected override void TryDeserializeInnerPayload(ref PacketReader reader)
    {
        // no-op
    }

    protected override void SerializeInnerPayload(BinaryWriter writer)
    {
        // no-op
    }
}