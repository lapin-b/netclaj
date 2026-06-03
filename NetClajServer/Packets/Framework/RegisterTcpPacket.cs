using NetClajServer.Datastructures;
using NetClajServer.Packets.IO;

namespace NetClajServer.Packets.Framework;

public class RegisterTcpPacket: MindustryPacket, ISequenceDeserializable
{
    public int ConnectionId { get; set; }

    public const sbyte Type = PacketType.Framework;
    public const byte Identifier = 4;

    public override sbyte GetPacketFamily() => Type;
    public override byte GetPacketIdentifier() => Identifier;

    public override void Deserialize(BinaryReader reader)
    {
        ConnectionId = reader.ReadInt32BigEndian();
    }

    public override void Serialize(BinaryWriter writer)
    {
        writer.WriteInt32BigEndian(ConnectionId);
    }

    public PacketResult TryDeserialize(ref PacketReader reader)
    {
        ConnectionId = reader.ReadConnectionId(nameof(ConnectionId));
        return reader.Result;
    }
}