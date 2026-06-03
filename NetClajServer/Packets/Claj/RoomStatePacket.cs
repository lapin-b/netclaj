using System.Buffers;
using NetClajServer.Datastructures;
using NetClajServer.Packets.IO;

namespace NetClajServer.Packets.Claj;

public class RoomStatePacket: MindustryPacket, ISequenceDeserializable
{
    public const sbyte Type = PacketType.Claj;
    public const byte Identifier = 14;

    public ReadOnlySequence<byte> StateBuffer { get; set; }
    
    public override sbyte GetPacketFamily() => Type;

    public override byte GetPacketIdentifier() => Identifier;

    public override void Deserialize(BinaryReader reader)
    {
        throw new NotSupportedException();
    }

    public override void Serialize(BinaryWriter writer)
    {
        writer.WriteUInt16BigEndian((ushort)StateBuffer.Length);
        foreach (var segment in StateBuffer)
        {
            writer.Write(segment.Span);
        }
    }

    public PacketResult TryDeserialize(ref PacketReader reader)
    {
        reader.WithPacketName(nameof(RoomStatePacket));
        
        var bufferLength = reader.ReadShortBigEndian(nameof(StateBuffer)).Value;
        StateBuffer = reader.ReadExactBytes(nameof(StateBuffer), bufferLength);
        
        return reader.Result;
    }
}