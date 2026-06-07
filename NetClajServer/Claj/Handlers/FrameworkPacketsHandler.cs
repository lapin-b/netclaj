using System.Diagnostics.CodeAnalysis;
using System.Net;
using Microsoft.Extensions.Logging;
using NetClajServer.Claj.PacketHandling;
using NetClajServer.Mindustry;
using NetClajServer.Packets.Framework;

namespace NetClajServer.Claj.Handlers;

public class FrameworkPacketsHandler: IPacketHandler<PingPacket>, 
    IPacketHandler<DiscoverHostPacket>, 
    IPacketHandler<KeepAlivePacket>
{
    private static readonly byte[] HostDiscoveryReply = [0xFC, 0, 0, 0, 4];

    public ValueTask HandleAsync(PacketContext context, PingPacket packet)
    {
        return context.Connection.Send(new PingPacket()
        {
            Id = packet.Id,
            IsReply = true
        }, context.IsTcp);
    }

    public ValueTask HandleAsync(PacketContext context, DiscoverHostPacket packet)
    {
        return context.IsTcp
            // Apparently the discovery packet is sent over UDP ?
            ? ValueTask.CompletedTask
            // The Java implementation just sends the bunch of bytes,skipping the "packet identifier" thing
            // entirely.
            : context.Connection.SendUdp(HostDiscoveryReply);
    }

    public ValueTask HandleAsync(PacketContext context, KeepAlivePacket packet)
    {
        return context.Connection.Send(new KeepAlivePacket(), context.IsTcp);
    }
}