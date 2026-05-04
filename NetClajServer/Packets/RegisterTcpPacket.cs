using NetClajServer.Datastructures;

namespace NetClajServer.Packets;

public class RegisterTcpPacket: IMindustryPacket
{
    public int ConnectionId { get; set; }

    public const sbyte Type = PacketType.Framework;
    public const byte Identifier = 4;

    sbyte IMindustryPacket.GetPacketType() => Type;
    public byte GetPacketIdentifier() => Identifier;

    public void Deserialize(BinaryReader reader)
    {
        ConnectionId = reader.ReadInt32BigEndian();
    }

    public void Serialize(BinaryWriter writer)
    {
        writer.WriteInt32BigEndian(ConnectionId);
    }
}