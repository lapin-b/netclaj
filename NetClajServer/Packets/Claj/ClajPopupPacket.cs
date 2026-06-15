using System.Buffers;
using NetClajServer.Packets.IO;

namespace NetClajServer.Packets.Claj;

public class ClajPopupPacket: MindustryPacket
{
    public string Message { get; set; }
    
    public const sbyte Type = PacketType.Claj;
    public const byte Identifier = 23;

    public override sbyte GetPacketFamily() => Type;
    public override byte GetPacketIdentifier() => Identifier;

    public override void Serialize(IBufferWriter<byte> writer)
    {
        writer.WriteJavaUtf(Message);
    }

    public override PacketResult TryDeserialize(ref PacketReader reader)
    {
        Message = reader.ReadJavaUtf(nameof(Message));
        return reader.Result;
    }
}