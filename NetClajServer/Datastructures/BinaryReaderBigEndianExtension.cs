using System.Buffers.Binary;

namespace NetClajServer.Datastructures;

// https://github.com/dotnet/runtime/issues/26904#issuecomment-2292530322
public static class BinaryReaderBigEndianExtension
{
    public static short ReadInt16BigEndian(this BinaryReader br)
    {
        return BinaryPrimitives.ReadInt16BigEndian(br.ReadSpan(stackalloc byte[sizeof(short)]));
    }
    
    public static ushort ReadUInt16BigEndian(this BinaryReader br)
    {
        return BinaryPrimitives.ReadUInt16BigEndian(br.ReadSpan(stackalloc byte[sizeof(ushort)]));
    }

    public static int ReadInt32BigEndian(this BinaryReader br)
    {
        return BinaryPrimitives.ReadInt32BigEndian(br.ReadSpan(stackalloc byte[sizeof(int)]));
    }

    private static ReadOnlySpan<byte> ReadSpan(this BinaryReader br, Span<byte> buffer)
    {
        br.BaseStream.ReadExactly(buffer);
        return buffer;
    }
}