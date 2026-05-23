using System.Buffers;
using Microsoft.Extensions.Logging;

namespace NetClajServer.Claj;

public partial class Room
{
    [LoggerMessage(LogLevel.Debug, "{RoomId} H -> P: {HostId} relaying to {targetId} {payload}")]
    partial void LogHostToClientPayloadRelay(long roomId, int hostId, int targetId, ReadOnlySequence<byte> payload);

    [LoggerMessage(LogLevel.Debug, "P -> H {RoomId}: {sourceId} relaying to {hostId} {payload}")]
    partial void LogClientToHostPayloadRelay(long roomId, int sourceId, int hostId, ReadOnlyMemory<byte> payload);
}