using System.Net.Mime;
using NetClajServer.Claj.PacketHandling;
using NetClajServer.Packets.Claj;

namespace NetClajServer.Claj.Handlers;

public class CreateClajRoomRequestHandler : IPacketHandler<RoomCreateRequestPacket>
{
    public async Task HandleAsync(PacketContext context, RoomCreateRequestPacket packet)
    {
        var serverVersion = new Version(3, 0, 0);
        var remoteVersion = new Version(packet.Version);

        var versionResult = remoteVersion.CompareTo(serverVersion);

        if (versionResult < 0)
        {
            // Client version is too old, deny link creation
            await context.Connection.SendTcp(new ClajMessagePacket()
            {
                Message = "Your CLaJ version is outdated, please update it by reinstalling the 'claj' mod."
            });
            
            await context.Connection.Close();
        }
    }
}