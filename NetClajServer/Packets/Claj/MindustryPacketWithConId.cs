using NetClajServer.Datastructures;
using NetClajServer.Packets.IO;

namespace NetClajServer.Packets.Claj;

public abstract class MindustryPacketWithConId: MindustryPacket, ISequenceDeserializable
{
    public int ConnectionId { get; set; }
    
    public const sbyte Type = PacketType.Claj;

    public override sbyte GetPacketFamily() => Type;
    public abstract override byte GetPacketIdentifier();

    public override void Deserialize(BinaryReader reader)
    {
        throw new NotSupportedException();
    }

    public override void Serialize(BinaryWriter writer)
    {
        writer.WriteInt32BigEndian(ConnectionId);
        SerializeInnerPayload(writer);
    }
    
    public PacketResult TryDeserialize(ref PacketReader reader)
    {
        ConnectionId = reader.NeedIntBigEndian(nameof(ConnectionId));
        TryDeserializeInnerPayload(ref reader);
        
        return reader.Result;
    }

    protected abstract void TryDeserializeInnerPayload(ref PacketReader reader);
    protected abstract void SerializeInnerPayload(BinaryWriter writer);
}