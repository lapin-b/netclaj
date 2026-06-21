using PacketHandling;

namespace NetClajServer.Claj.PacketHandling;

public interface IPacketHandler<TPacket> 
    where TPacket: MindustryPacket
{
    ValueTask HandleAsync(PacketContext context, TPacket packet);
}