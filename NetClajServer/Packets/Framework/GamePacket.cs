using System.Buffers;
using NetClajServer.Datastructures;
using NetClajServer.Packets.IO;

namespace NetClajServer.Packets.Framework;

public class GamePacket: MindustryPacket, ISequenceDeserializable
{
    public ReadOnlySequence<byte> Buffer { get; set; }

    public GamePacket()
    {
        Buffer = ReadOnlySequence<byte>.Empty;
    }

    public GamePacket(byte[] buffer)
    {
        Buffer = new ReadOnlySequence<byte>(buffer);
    }

    public override sbyte GetPacketFamily() => sbyte.MaxValue;
    public override byte GetPacketIdentifier() => byte.MaxValue;
    
    public override void Deserialize(BinaryReader reader)
    {
        throw new NotSupportedException();
    }
    
    public PacketResult TryDeserialize(ref PacketReader reader)
    {
        reader.NeedRest(out var bytes);
        if (reader.ProcessingFailed) return reader.Result;

        Buffer = bytes;
        return PacketResult.Ok();
    }

    public override void Serialize(BinaryWriter writer)
    {
        foreach (var segment in Buffer)
        {
            writer.Write(segment.Span);
        }
    }
}