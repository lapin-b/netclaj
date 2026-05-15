namespace NetClajServer.Packets.Claj;

public class RoomJoinAcceptedPacket: RoomLinkPacket
{
    private new const sbyte Type = PacketType.Claj;
    public new const byte Identifier = 9;
}