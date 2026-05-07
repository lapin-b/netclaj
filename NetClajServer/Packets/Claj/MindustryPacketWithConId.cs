using NetClajServer.Datastructures;

namespace NetClajServer.Packets.Claj;

public abstract class MindustryPacketWithConId: IMindustryPacket
{
    public int ConnectionId { get; set; }
    
    public const sbyte Type = PacketType.Claj;

    sbyte IMindustryPacket.GetPacketType() => Type;
    public abstract byte GetPacketIdentifier();

    public void Deserialize(BinaryReader reader)
    {
        reader.ReadInt32BigEndian();
        DeserializeInnerPayload(reader);
    }

    public void Serialize(BinaryWriter writer)
    {
        writer.WriteInt32BigEndian(ConnectionId);
        SerializeInnerPayload(writer);
    }

    protected abstract void DeserializeInnerPayload(BinaryReader reader);
    protected abstract void SerializeInnerPayload(BinaryWriter writer);
}