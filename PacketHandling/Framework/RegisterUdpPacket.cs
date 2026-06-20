using System.Buffers;
using PacketHandling.IO;

namespace PacketHandling.Framework;

public class RegisterUdpPacket: MindustryPacket
{
    public int ConnectionId { get; set; }

    public const sbyte Type = PacketType.Framework;
    public const byte Identifier = 3;

    public override sbyte GetPacketFamily() => Type;
    public override byte GetPacketIdentifier() => Identifier;

    public override void Serialize(IBufferWriter<byte> writer)
    {
        writer.WriteInt32BigEndian(ConnectionId);
    }


    public override PacketResult TryDeserialize(ref PacketReader reader)
    {
        ConnectionId = reader.ReadConnectionId(nameof(ConnectionId));
        return reader.Result;
    }
}