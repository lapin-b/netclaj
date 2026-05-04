using System.Net.Sockets;

namespace NetClajServer.Packets;

public interface IMindustryPacket
{
    public sbyte GetPacketType();
    public byte GetPacketIdentifier();
    public void Deserialize(BinaryReader reader);
    public void Serialize(BinaryWriter writer);
}