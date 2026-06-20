using PacketHandling.Streaming;

namespace NetClajServer.Packets.Streaming;

public interface IStreamablePacket
{
    public int StreamTotalPacketSize();
    public ValueTask StreamChunks(IStreamSink sink);
    public byte GetPacketIdentifier();
}