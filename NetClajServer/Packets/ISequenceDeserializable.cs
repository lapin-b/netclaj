using System.Buffers;

namespace NetClajServer.Packets;

public interface ISequenceDeserializable
{
    public void Deserialize(ref SequenceReader<byte> reader);
}