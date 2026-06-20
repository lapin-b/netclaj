using System.Buffers;
using Microsoft.Extensions.Logging;
using PacketHandling;
using PacketHandling.Framework;

namespace NetClajServer.Mindustry;

public partial class Connection
{
    [LoggerMessage(LogLevel.Debug, "{ConnectionID} Received bytes {bytes}")]
    partial void LogBytesRecv(int connectionId, byte[] bytes);

    [LoggerMessage(LogLevel.Debug, "{ConnectionID} Got packet type {packetType}")]
    partial void LogPacketTypeRecv(int connectionId, string packetType);

    [LoggerMessage(LogLevel.Debug, "{ConnectionId} not yet participating in a room. Enqueueing raw packet")]
    partial void LogNotYetParticipatingInRoom(int connectionId);
    
    [LoggerMessage(LogLevel.Trace, "{Transport}: {connectionId} Sending {bytes}")] 
    partial void LogSentBytes(string transport, int connectionId, ReadOnlyMemory<byte> bytes);

    void DebugRecvBytes(ReadOnlySequence<byte> payload)
    {
        if (!_logger.IsEnabled(LogLevel.Debug))
        {
            return;
        }

        var isPingPacket = false;
        byte[]? materializedPayload = null;
        
        var familyIdentifierBytes = payload.Slice(0, 2);
        if (familyIdentifierBytes.IsSingleSegment)
        {
            var segment = familyIdentifierBytes.FirstSpan;
            isPingPacket = segment[0] == 0xFE && segment[1] == 0x00;
        }
        else
        {
            materializedPayload = payload.ToArray();
            isPingPacket = materializedPayload[0] == 0xFE && materializedPayload[1] == 0x00;
        }

        if (isPingPacket)
        {
            return;
        }

        materializedPayload ??= payload.ToArray();
        LogBytesRecv(Id, materializedPayload);
    }

    void DebugRecvPacket(MindustryPacket packet)
    {
        if (!_logger.IsEnabled(LogLevel.Debug))
        {
            return;
        }

        if (packet is PingPacket or KeepAlivePacket)
        {
            return;
        }
        
        LogPacketTypeRecv(Id, packet.GetType().Name);
    }
}