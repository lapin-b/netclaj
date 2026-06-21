using System.Buffers;

namespace PacketHandling.IO;

public interface IArrayBufferSerializable
{
    public void Serialize(ArrayBufferWriter<byte> writer);
}