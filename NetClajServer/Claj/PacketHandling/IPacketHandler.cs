using NetClajServer.Packets;

namespace NetClajServer.Claj.PacketHandling;

public interface IPacketHandler<TPacket> 
    where TPacket: MindustryPacket
{
    Task HandleAsync(PacketContext context, TPacket packet);
}