
namespace NetClajServer.Packets.Claj;

// Exists for compatibility with older claj client versions
public class RoomJoinRequestPacket: RoomJoinPacket
{
    public new const sbyte Type = PacketType.Claj;
    public new const byte Identifier = 8;

    public RoomJoinPacket AsRoomJoinPacket => this;
}