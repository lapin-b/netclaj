namespace NetClajServer.Packets.Streaming;

public interface IStreamablePacket
{
    public int StreamTotalPacketSize();
    public Task StreamChunks(TcpStreamSink sink);
    public byte GetPacketIdentifier();
}