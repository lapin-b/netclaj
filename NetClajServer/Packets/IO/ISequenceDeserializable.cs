using System.Buffers;

namespace NetClajServer.Packets.IO;

public interface ISequenceDeserializable
{
    // TODO: Rename to TryDeserialize and return a boolean instead of throwing exceptions
    public PacketResult TryDeserialize(ref PacketReader reader);
}