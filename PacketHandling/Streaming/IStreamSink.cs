namespace PacketHandling.Streaming;

public interface IStreamSink
{
    public ValueTask Write(ReadOnlyMemory<byte> payload);
}