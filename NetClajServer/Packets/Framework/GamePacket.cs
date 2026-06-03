using System.Buffers;
using NetClajServer.Packets.IO;

namespace NetClajServer.Packets.Framework;

public class GamePacket: MindustryPacket, ISequenceDeserializable
{
    public ReadOnlySequence<byte> Buffer { get; set; }

    public GamePacket()
    {
        Buffer = ReadOnlySequence<byte>.Empty;
    }

    public override sbyte GetPacketFamily() => sbyte.MaxValue;
    public override byte GetPacketIdentifier() => byte.MaxValue;
    
    public override void Deserialize(BinaryReader reader)
    {
        throw new NotSupportedException();
    }
    
    public PacketResult TryDeserialize(ref PacketReader reader)
    {
        Buffer = reader.ReadRest();
        return reader.Result;
    }

    public override void Serialize(BinaryWriter writer)
    {
        foreach (var segment in Buffer)
        {
            writer.Write(segment.Span);
        }
    }
}