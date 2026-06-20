using System.Buffers;
using PacketHandling.IO;

namespace PacketHandling.Framework;

public class MaterializedGamePacket: GamePacket
{
    public new ReadOnlyMemory<byte> Buffer { get; set; }
    
    public MaterializedGamePacket(GamePacket packet)
    {
        TransportIsTcp = packet.TransportIsTcp;
        Buffer = packet.Buffer.ToArray();
    }

    public override PacketResult TryDeserialize(ref PacketReader reader)
    {
        throw new NotSupportedException();
    }

    public override void Serialize(IBufferWriter<byte> writer)
    {
        writer.Write(Buffer.Span);
    }
}