using System.Buffers;
using Microsoft.Extensions.Logging;
using NetClajServer.Mindustry;
using NetClajServer.Packets;

namespace NetClajServer.Claj;

public partial class Room
{
    [LoggerMessage(LogLevel.Debug, "{RoomId} H -> P: {HostId} relaying to {targetId}")]
    private partial void LogHostToClientPayloadRelay(long roomId, int hostId, int targetId);

    [LoggerMessage(LogLevel.Debug, "{RoomId} P -> H: {sourceId} relaying to {hostId}")]
    private partial void LogClientToHostPayloadRelay(long roomId, int sourceId, int hostId);
}