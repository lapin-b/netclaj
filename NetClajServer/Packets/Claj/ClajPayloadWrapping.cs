using System.Buffers;
using NetClajServer.Packets.IO;

namespace NetClajServer.Packets.Claj;

public class ClajPayloadWrapping: MindustryPacketWithConId
{
    public bool WrappedPacketIsTcp { get; set; }
    public ReadOnlySequence<byte> Buffer { get; set; } = ReadOnlySequence<byte>.Empty;
    
    public const byte Identifier = 2;
    public override byte GetPacketIdentifier() => Identifier;

    protected override void TryDeserializeInnerPayload(ref PacketReader reader)
    {
        const string packetName = nameof(ClajPayloadWrapping);
        reader.NeedBoolean(packetName, nameof(WrappedPacketIsTcp), out var packetIsTcp);
        reader.NeedRest(out var buffer);

        WrappedPacketIsTcp = packetIsTcp;
        Buffer = buffer;
    }

    protected override void SerializeInnerPayload(BinaryWriter writer)
    {
        writer.Write(WrappedPacketIsTcp);
        foreach (var fragment in Buffer)
        {
            writer.Write(fragment.Span);
        }
    }
}