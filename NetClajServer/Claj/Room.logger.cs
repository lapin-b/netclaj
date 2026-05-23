using System.Buffers;
using Microsoft.Extensions.Logging;
using NetClajServer.Mindustry;
using NetClajServer.Packets;

namespace NetClajServer.Claj;

public partial class Room
{
    [LoggerMessage(LogLevel.Debug, "{RoomId} H -> P: {HostId} relaying to {targetId} {payload}")]
    private partial void LogHostToClientPayloadRelay(long roomId, int hostId, int targetId, ReadOnlySequence<byte> payload);

    private void LogClientToHostPayloadRelay(long roomId, int sourceId, int hostId, MindustryPacket packet)
    {
        if (!_logger.IsEnabled(LogLevel.Debug))
        {
            return;
        }

        var payload = Serializer.Serialize(packet);
        _logger.LogDebug("P -> H {RoomId}: {sourceId} relaying to {hostId} {payload}", roomId, sourceId, hostId, payload);
    }
}