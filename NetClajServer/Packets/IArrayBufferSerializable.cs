using System.Buffers;

namespace NetClajServer.Packets;

public interface IArrayBufferSerializable
{
    public void Serialize(ArrayBufferWriter<byte> writer);
}