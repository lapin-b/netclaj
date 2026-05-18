using System.Buffers;

namespace NetClajServer.Packets.IO;

public interface ISequenceDeserializable
{
    public PacketResult TryDeserialize(ref PacketReader reader);
}