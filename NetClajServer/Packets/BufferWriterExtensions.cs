using System.Buffers;
using System.Numerics;

namespace NetClajServer.Packets;

public static class BufferWriterExtensions
{
    extension(IBufferWriter<byte> buffer)
    {
        public void WriteIntegerBe<T>(T value) where T : IBinaryInteger<T>
        {
            var span = buffer.GetSpan(value.GetByteCount());
            value.WriteBigEndian(span);
            buffer.Advance(span.Length);
        }
    }
}