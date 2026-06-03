using NetClajServer.Datastructures;
using NetClajServer.Packets.IO;

namespace NetClajServer.Packets.Claj;

public class ConnectionJoinPacket: MindustryPacketWithConId
{
    public long RoomId { get; set; }
    
    public const byte Identifier = 0;

    public override byte GetPacketIdentifier() => Identifier;
    
    protected override void TryDeserializeInnerPayload(ref PacketReader reader)
    {
        reader.WithPacketName(nameof(ConnectionJoinPacket));
        RoomId = reader.NeedRoomId(nameof(RoomId));
    }

    protected override void SerializeInnerPayload(BinaryWriter writer)
    {
        writer.WriteInt64BigEndian(RoomId);
    }
}