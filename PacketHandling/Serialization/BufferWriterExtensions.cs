using System.Buffers;
using System.Numerics;
using PacketHandling.IO;

namespace PacketHandling.Serialization;

public static class BufferWriterExtensions
{
    extension(IBufferWriter<byte> buffer)
    {
        public void WriteIntegerBe<T>(T value) where T : IBinaryInteger<T>
        {
            var bytesCount = value.GetByteCount();

            var span = buffer.GetSpan(bytesCount);
            value.WriteBigEndian(span[..bytesCount]);
            buffer.Advance(bytesCount);
        }

        public void WriteInt32BigEndian(int n) => buffer.WriteIntegerBe(n);
        
        public void WriteBool(bool b) => buffer.WriteIntegerBe((byte)(b ? 1 : 0));

        public void WriteJavaUtf(string str)
        {
            var encodedString = JavaDataObjectStream.EncodeUtf(str);
            buffer.WriteIntegerBe((short)encodedString.Length);
            buffer.Write(encodedString);
        }
    }
}