using Microsoft.Extensions.Logging;
using NetClajServer.Mindustry;

namespace NetClajServer.Claj.PacketHandling;

public class PacketContext
{
    public required Connection Connection { get; init; }
    public required CancellationToken CancellationToken { get; init; }
    public required bool IsTcp { get; init; }
}