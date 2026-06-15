using System.Buffers;

namespace NetClajServer.Packets;

public class CountingBufferWritter: IBufferWriter<byte>
{
    private readonly IBufferWriter<byte> _inner;
    public int BytesWritten { get; private set; }
    
    public CountingBufferWritter(IBufferWriter<byte> inner)
    {
        _inner = inner;
    }


    public void Advance(int count)
    {
        _inner.Advance(count);
        BytesWritten += count;
    }

    public Memory<byte> GetMemory(int sizeHint = 0) => _inner.GetMemory(sizeHint);
    public Span<byte> GetSpan(int sizeHint = 0) => _inner.GetSpan(sizeHint);
}