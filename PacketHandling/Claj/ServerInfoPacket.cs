using System.Buffers;
using PacketHandling.IO;

namespace PacketHandling.Claj;

public class ServerInfoPacket: MindustryPacket
{
    public const sbyte Type = PacketType.Claj;
    public const byte Identifier = 20;

    public override sbyte GetPacketFamily() => Type;
    public override byte GetPacketIdentifier() => Identifier;

    public override void Serialize(IBufferWriter<byte> writer)
    {
        throw new NotImplementedException();
    }

    public override PacketResult TryDeserialize(ref PacketReader reader)
    {
        throw new NotImplementedException();
    }
}