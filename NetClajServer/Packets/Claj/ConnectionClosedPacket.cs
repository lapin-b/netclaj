using NetClajServer.Datastructures;

namespace NetClajServer.Packets.Claj;

public class ConnectionClosedPacket: MindustryPacketWithConId
{
    public ConnectionCloseReason Reason { get; set; }
    
    public const byte Identifier = 1;

    public override byte GetPacketIdentifier() => Identifier;

    protected override void DeserializeInnerPayload(BinaryReader reader)
    {
        Reason = (ConnectionCloseReason)reader.ReadInt32BigEndian();
    }

    protected override void SerializeInnerPayload(BinaryWriter writer)
    {
        writer.WriteInt32BigEndian((int)Reason);
    }
}