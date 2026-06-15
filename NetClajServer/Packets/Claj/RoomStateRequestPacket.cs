using System.Buffers;
using NetClajServer.Packets.IO;

namespace NetClajServer.Packets.Claj;

public class RoomStateRequestPacket: MindustryPacket
{
    public const sbyte Type = PacketType.Claj;
    public const byte Identifier = 13;
    
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