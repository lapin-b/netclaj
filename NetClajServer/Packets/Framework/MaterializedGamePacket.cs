using System.Buffers;
using NetClajServer.Packets.IO;

namespace NetClajServer.Packets.Framework;

public class MaterializedGamePacket: GamePacket
{
    public new ReadOnlyMemory<byte> Buffer { get; set; }
    
    public MaterializedGamePacket(GamePacket packet)
    {
        IsTcp = packet.IsTcp;
        Buffer = packet.Buffer.ToArray();
    }

    public override PacketResult TryDeserialize(ref PacketReader reader)
    {
        throw new NotSupportedException();
    }

    public override void Serialize(BinaryWriter writer)
    {
        writer.Write(Buffer.Span);
    }
}