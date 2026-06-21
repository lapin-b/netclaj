using System.Buffers;
using PacketHandling.IO;
using PacketHandling.Serialization;

namespace PacketHandling.Claj;

public abstract class MindustryPacketWithConId: MindustryPacket
{
    public int ConnectionId { get; set; }
    
    public const sbyte Type = PacketType.Claj;

    public override sbyte GetPacketFamily() => Type;
    public abstract override byte GetPacketIdentifier();

    public override void Serialize(IBufferWriter<byte> writer)
    {
        writer.WriteInt32BigEndian(ConnectionId);
        SerializeInnerPayload(writer);
    }
    
    public override PacketResult TryDeserialize(ref PacketReader reader)
    {
        ConnectionId = reader.ReadIntBigEndian(nameof(ConnectionId));
        TryDeserializeInnerPayload(ref reader);
        
        return reader.Result;
    }

    protected abstract void TryDeserializeInnerPayload(ref PacketReader reader);
    protected abstract void SerializeInnerPayload(IBufferWriter<byte> writer);
}