using NetClajServer.Datastructures;

namespace NetClajServer.Packets.Streaming;

public class StreamChunk: MindustryPacket
{
    public int StreamId { get; set; }
    public bool IsLastChunk { get; set; }
    
    // TODO: Make the chunk a span of bytes
    public ReadOnlyMemory<byte> Chunk { get; set; }
    
    public const sbyte Type = PacketType.Claj;
    public const byte Identifier = 25;

    public override sbyte GetPacketFamily() => Type;
    public override byte GetPacketIdentifier() => Identifier;
    
    public override void Deserialize(BinaryReader reader)
    {
        throw new NotImplementedException();
    }

    public override void Serialize(BinaryWriter writer)
    {
        writer.WriteInt32BigEndian(StreamId);
        writer.Write(IsLastChunk);
        writer.Write(Chunk.Span);
    }
}