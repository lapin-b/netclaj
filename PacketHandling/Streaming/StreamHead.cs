using System.Buffers;
using PacketHandling.IO;

namespace PacketHandling.Streaming;

public class StreamHead: MindustryPacket
{
    private static int _streamId = 1;
    
    public int Id { get; }
    public int TotalBytes { get; set; }
    public byte InnerPacketIdentifier { get; set; }
    public bool IsCompressed { get; set; }
    
    public const sbyte Type = PacketType.Claj;
    public const byte Identifier = 24;

    public StreamHead()
    {
        // For now, not being thread-safe is not the end of the world. If this packet were used more often,
        // consider making the constructor thread-safe.
        Id = _streamId;
        _streamId += 1;
    }

    public override sbyte GetPacketFamily() => Type;
    public override byte GetPacketIdentifier() => Identifier;
    
    public override PacketResult TryDeserialize(ref PacketReader reader)
    {
        // A stream is only sent from the server to the client
        throw new NotSupportedException();
    }

    public override void Serialize(IBufferWriter<byte> writer)
    {
        writer.WriteInt32BigEndian(Id);
        writer.WriteInt32BigEndian(TotalBytes);
        writer.WriteIntegerBe(InnerPacketIdentifier);
        writer.WriteBool(IsCompressed);
    }
}