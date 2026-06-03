using NetClajServer.Datastructures;
using NetClajServer.Packets.IO;

namespace NetClajServer.Packets.Streaming;

public class StreamChunk: MindustryPacket
{
    public int StreamId { get; set; }
    public bool IsLastChunk { get; set; }
    
    public ReadOnlyMemory<byte> Chunk { get; set; }
    
    public const sbyte Type = PacketType.Claj;
    public const byte Identifier = 25;

    public override sbyte GetPacketFamily() => Type;
    public override byte GetPacketIdentifier() => Identifier;
    public override PacketResult TryDeserialize(ref PacketReader reader)
    {
        // A stream is only sent from the server to the client
        throw new NotSupportedException();
    }

    public override void Serialize(BinaryWriter writer)
    {
        writer.WriteInt32BigEndian(StreamId);
        writer.Write(IsLastChunk);
        writer.WriteInt16BigEndian((short)Chunk.Length);
        writer.Write(Chunk.Span);
    }
}