using NetClajServer.Datastructures;
using NetClajServer.Packets.IO;

namespace NetClajServer.Packets.Claj;

public class ClajTextMessagePacket: MindustryPacket
{
    public string Message { get; set; }
    
    public const sbyte Type = PacketType.Claj;
    public const byte Identifier = 21;

    public override sbyte GetPacketFamily() => Type;
    public override byte GetPacketIdentifier() => Identifier;

    public override void Serialize(BinaryWriter writer)
    {
        writer.WriteJavaUtf(Message);
    }

    public override PacketResult TryDeserialize(ref PacketReader reader)
    {
        Message = reader.ReadJavaUtf(nameof(Message));
        return reader.Result;
    }
}