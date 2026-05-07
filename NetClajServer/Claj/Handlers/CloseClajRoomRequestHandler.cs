using NetClajServer.Claj.PacketHandling;
using NetClajServer.Packets.Claj;

namespace NetClajServer.Claj.Handlers;

public class CloseClajRoomRequestHandler: IPacketHandler<RoomCloseRequestPacket>
{
    public Task HandleAsync(PacketContext context, RoomCloseRequestPacket packet)
    {
        throw new NotImplementedException();
    }
}