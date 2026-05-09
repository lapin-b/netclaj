using Microsoft.Extensions.Logging;

namespace NetClajServer.Mindustry;

public partial class Connection
{
    [LoggerMessage(LogLevel.Debug, "{ConnectionID} Received bytes {bytes}")]
    partial void LogBytesRecv(int connectionId, byte[] bytes);

    [LoggerMessage(LogLevel.Debug, "{ConnectionID} Got packet type {packetType}")]
    partial void LogPacketTypeRecv(int connectionId, string packetType);

    [LoggerMessage(LogLevel.Debug, "{ConnectionId} not yet participating in a room. Enqueueing raw packet")]
    partial void LogNotYetParticipatingInRoom(int connectionId);
}