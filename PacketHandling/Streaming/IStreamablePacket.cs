namespace PacketHandling.Streaming;

public interface IStreamablePacket
{
    public int StreamTotalPacketSize();
    public ValueTask StreamChunks(IStreamSink sink);
    public byte GetPacketIdentifier();
}