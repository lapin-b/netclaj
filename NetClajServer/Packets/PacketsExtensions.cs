using NetClajServer.Claj;
using PacketHandling.Claj;

namespace NetClajServer.Packets;

public static class PacketsExtensions
{
    public static RoomConfiguration IntoRoomConfiguration(this RoomConfigPacket packet)
    {
        return new RoomConfiguration
        {
            IsPublic = packet.IsPublic,
            IsProtectedByPin = packet.IsProtectedByPin,
            CanRequestHostState = packet.CanRequestHostState,
            Pin = packet.Pin,
            MaxClients = packet.MaxClients
        };
    }
}