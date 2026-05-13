
namespace NetClajServer.Datastructures;

public static class StreamToByteArrayExtension
{
    public static byte[] ReadToByteArray(this Stream stream)
    {
        using var tempBuffer = new MemoryStream();
        stream.CopyTo(tempBuffer);
        return tempBuffer.ToArray();
    }

    public static bool HasBytesRemaining(this Stream stream) => stream.Position < stream.Length;
}