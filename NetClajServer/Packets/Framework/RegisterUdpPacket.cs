using NetClajServer.Datastructures;

namespace NetClajServer.Packets.Framework;

public class RegisterUdpPacket: MindustryPacket
{
    public int ConnectionId { get; set; }

    public const sbyte Type = PacketType.Framework;
    public const byte Identifier = 3;

    public override sbyte GetPacketType() => Type;
    public override byte GetPacketIdentifier() => Identifier;

    public override void Deserialize(BinaryReader reader)
    {
        ConnectionId = reader.ReadInt32BigEndian();
    }

    public override void Serialize(BinaryWriter writer)
    {
        writer.WriteInt32BigEndian(ConnectionId);
    }
}