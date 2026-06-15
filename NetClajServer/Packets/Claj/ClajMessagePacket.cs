using System.Buffers;
using NetClajServer.Claj;
using NetClajServer.Packets.IO;

namespace NetClajServer.Packets.Claj;

public class ClajMessagePacket: MindustryPacket
{
    public const sbyte Type = PacketType.Claj;
    public const byte Identifier = 22;
    public ClajMessages Message { get; set; }

    public override sbyte GetPacketFamily() => Type;
    public override byte GetPacketIdentifier() => Identifier;

    public override void Serialize(IBufferWriter<byte> writer)
    {
        writer.WriteInt32BigEndian((int)Message);
    }

    public override PacketResult TryDeserialize(ref PacketReader reader)
    {
        Message = (ClajMessages)reader.ReadIntBigEndian(nameof(Message)).Value;
        return reader.Result;
    }
}