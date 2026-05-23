namespace NetClajServer.Packets.Streaming;

public interface IStreamablePacket
{
    public int StreamTotalPacketSize();
    public ValueTask StreamChunks(TcpStreamSink sink);
    public byte GetPacketIdentifier();
}