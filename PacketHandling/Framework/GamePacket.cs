using System.Buffers;
using NetClajServer.Packets.IO;

namespace NetClajServer.Packets.Framework;

public class GamePacket: MindustryPacket
{
    public ReadOnlySequence<byte> Buffer { get; set; }

    public GamePacket()
    {
        Buffer = ReadOnlySequence<byte>.Empty;
    }

    public override sbyte GetPacketFamily() => sbyte.MaxValue;
    public override byte GetPacketIdentifier() => byte.MaxValue;

    public override PacketResult TryDeserialize(ref PacketReader reader)
    {
        Buffer = reader.ReadRest();
        return reader.Result;
    }

    public override void Serialize(IBufferWriter<byte> writer)
    {
        foreach (var segment in Buffer)
        {
            writer.Write(segment.Span);
        }
    }
}