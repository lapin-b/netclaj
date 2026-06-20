using System.Buffers;
using NetClajServer.Packets.IO;

namespace NetClajServer.Packets.Framework;

public class RegisterTcpPacket: MindustryPacket
{
    public int ConnectionId { get; set; }

    public const sbyte Type = PacketType.Framework;
    public const byte Identifier = 4;

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