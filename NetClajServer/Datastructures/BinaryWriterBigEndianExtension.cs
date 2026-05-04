using System.Buffers.Binary;

namespace NetClajServer.Datastructures;

public static class BinaryWriterBigEndianExtension
{
    public static void WriteInt16BigEndian(this BinaryWriter bw, short val)
    {
        Span<byte> buffer = stackalloc byte[sizeof(short)];
        BinaryPrimitives.WriteInt16BigEndian(buffer, val);
        bw.Write(buffer);
    }

    public static void WriteInt32BigEndian(this BinaryWriter bw, int val)
    {
        Span<byte> buffer = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32BigEndian(buffer, val);
        bw.Write(buffer);
    }
}