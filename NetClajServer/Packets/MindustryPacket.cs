using System.Net.Sockets;

namespace NetClajServer.Packets;

public abstract class MindustryPacket
{
    public bool IsTcp { get; set; } = true;
    public abstract sbyte GetPacketType();
    public abstract byte GetPacketIdentifier();
    public abstract void Deserialize(BinaryReader reader);
    public abstract void Serialize(BinaryWriter writer);
}