namespace NetClajServer.Packets.Streaming;

public class StreamHead: MindustryPacket
{
    public int Id { get; set; }
    public int TotalBytes { get; set; }
    public byte InnerPacketIdentifier { get; set; }
    public bool IsCompressed { get; set; }
    
    public const sbyte Type = PacketType.Claj;
    public const byte Identifier = 24;

    public override sbyte GetPacketFamily() => Type;
    public override byte GetPacketIdentifier() => Identifier;
    
    public override void Deserialize(BinaryReader reader)
    {
        throw new NotImplementedException();
    }

    public override void Serialize(BinaryWriter writer)
    {
        throw new NotImplementedException();
    }
}