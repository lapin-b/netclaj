using System.Buffers;
using NetClajServer.Packets.IO;

namespace NetClajServer.Packets.Framework;

public class DiscoverHostPacket : MindustryPacket
{
    public const sbyte Type = PacketType.Framework;
    public const byte Identifier = 1;

    public override sbyte GetPacketFamily() => Type;
    public override byte GetPacketIdentifier() => Identifier;

    public override void Serialize(IBufferWriter<byte> writer)
    {
        // no-op
    }

    public override PacketResult TryDeserialize(ref PacketReader reader)
    {
        // no-op
        return reader.Result;
    }
}