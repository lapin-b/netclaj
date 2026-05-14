using System.Buffers;

namespace NetClajServer.Packets.IO;

public interface IArrayBufferSerializable
{
    public void Serialize(ArrayBufferWriter<byte> writer);
}