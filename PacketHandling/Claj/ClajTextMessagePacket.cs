using System.Buffers;
using PacketHandling.IO;
using PacketHandling.Serialization;

namespace PacketHandling.Claj;

public class ClajTextMessagePacket: MindustryPacket
{
    public string Message { get; set; }
    
    public const sbyte Type = PacketType.Claj;
    public const byte Identifier = 21;

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