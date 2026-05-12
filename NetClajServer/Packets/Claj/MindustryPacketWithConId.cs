using NetClajServer.Datastructures;

namespace NetClajServer.Packets.Claj;

public abstract class MindustryPacketWithConId: MindustryPacket
{
    public int ConnectionId { get; set; }
    
    public const sbyte Type = PacketType.Claj;

    public override sbyte GetPacketFamily() => Type;
    public abstract override byte GetPacketIdentifier();

    public override void Deserialize(BinaryReader reader)
    {
        ConnectionId = reader.ReadInt32BigEndian();
        DeserializeInnerPayload(reader);
    }

    public override void Serialize(BinaryWriter writer)
    {
        writer.WriteInt32BigEndian(ConnectionId);
        SerializeInnerPayload(writer);
    }

    protected abstract void DeserializeInnerPayload(BinaryReader reader);
    protected abstract void SerializeInnerPayload(BinaryWriter writer);
}