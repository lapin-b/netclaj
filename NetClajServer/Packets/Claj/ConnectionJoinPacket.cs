using NetClajServer.Datastructures;

namespace NetClajServer.Packets.Claj;

public class ConnectionJoinPacket: MindustryPacketWithConId
{
    public long RoomId { get; set; }
    
    public const byte Identifier = 2;

    public override byte GetPacketIdentifier() => Identifier;
    
    protected override void DeserializeInnerPayload(BinaryReader reader)
    {
        RoomId = reader.ReadInt64BigEndian();
    }

    protected override void SerializeInnerPayload(BinaryWriter writer)
    {
        writer.WriteInt64BigEndian(RoomId);
    }
}