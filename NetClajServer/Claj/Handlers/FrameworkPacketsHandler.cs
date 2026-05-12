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
    private readonly ILogger<FrameworkPacketsHandler> _logger;

    public FrameworkPacketsHandler(ILogger<FrameworkPacketsHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(PacketContext context, PingPacket packet)
    {
        return context.Connection.Send(new PingPacket()
        {
            Id = packet.Id,
            IsReply = true
        }, context.IsTcp);
    }

    public Task HandleAsync(PacketContext context, DiscoverHostPacket packet)
    {
        return context.Connection.Send(new DiscoverHostPacket(), context.IsTcp);
    }

    public Task HandleAsync(PacketContext context, KeepAlivePacket packet)
    {
        return context.Connection.Send(new KeepAlivePacket(), context.IsTcp);
    }

    public static bool TryRegisterUdpEndpoint(
        MindustryServer server, 
        IPEndPoint endpoint, 
        RegisterUdpPacket packet,
        [NotNullWhen(true)]
        out Connection? connection
    )
    {
        connection = null;
        if (!server.Connections.TryGetValue(packet.ConnectionId, out connection))
        {
            // Connection ID is not existant
            return false;
        }

        if (connection.UdpEndpoint != null)
        {
            // UDP endpoint is already tied with another endpoint
            return false;
        }

        connection.UdpEndpoint = endpoint;
        return true;
    }
}