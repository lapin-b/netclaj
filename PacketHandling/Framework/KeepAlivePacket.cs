using System.Buffers;
using PacketHandling.IO;

namespace PacketHandling.Framework;

public class KeepAlivePacket: MindustryPacket
{
    public const sbyte Type = PacketType.Framework;
    public const byte Identifier = 2;

    public override sbyte GetPacketFamily() => Type;
    public override byte GetPacketIdentifier() => Identifier;

    public override void Serialize(IBufferWriter<byte> writer)
    {
        // no-op
    }
    
    public override PacketResult TryDeserialize(ref PacketReader reader)
    {
        return reader.Result;
    }
}