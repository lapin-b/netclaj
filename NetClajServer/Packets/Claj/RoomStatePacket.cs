using System.Buffers;
using NetClajServer.Packets.IO;

namespace NetClajServer.Packets.Claj;

public class RoomStatePacket: MindustryPacket
{
    public const sbyte Type = PacketType.Claj;
    public const byte Identifier = 14;

    public ReadOnlySequence<byte> StateBuffer { get; set; }
    
    public override sbyte GetPacketFamily() => Type;

    public override byte GetPacketIdentifier() => Identifier;

    public override void Serialize(IBufferWriter<byte> writer)
    {
        writer.WriteIntegerBe((ushort)StateBuffer.Length);
        foreach (var segment in StateBuffer)
        {
            writer.Write(segment.Span);
        }
    }

    public override PacketResult TryDeserialize(ref PacketReader reader)
    {
        reader.WithPacketName(nameof(RoomStatePacket));
        
        var bufferLength = reader.ReadShortBigEndian(nameof(StateBuffer)).Value;
        StateBuffer = reader.ReadExactBytes(nameof(StateBuffer), bufferLength);
        
        return reader.Result;
    }
}