using System.Buffers;
using PacketHandling.IO;
using PacketHandling.Serialization;

namespace PacketHandling.Framework;

public class PingPacket: MindustryPacket
{
    public const sbyte Type = PacketType.Framework;
    public const byte Identifier = 0;

    public int Id { get; set; }
    public bool IsReply { get; set; }
    
    public override sbyte GetPacketFamily() => Type;
    public override byte GetPacketIdentifier() => Identifier;

    public override PacketResult TryDeserialize(ref PacketReader reader)
    {
        reader.WithPacketName(nameof(PingPacket));
        
        Id = reader.ReadIntBigEndian(nameof(Id));
        IsReply = reader.ReadBoolean(nameof(IsReply));

        return reader.Result;
    }

    public override void Serialize(IBufferWriter<byte> writer)
    {
        writer.WriteInt32BigEndian(Id);
        writer.WriteBool(IsReply);
    }
}