namespace NetClajServer.Packets.Framework;

public class RawPacket: MindustryPacket
{
    public byte[] Buffer { get; set; } = [];

    public RawPacket()
    {
        
    }

    public RawPacket(byte[] buffer)
    {
        Buffer = buffer;
    }

    public override sbyte GetPacketType() => sbyte.MaxValue;
    public override byte GetPacketIdentifier() => byte.MaxValue;

    public override void Deserialize(BinaryReader reader)
    {
        var tempStream = new MemoryStream((int)reader.BaseStream.Length);
        reader.BaseStream.CopyTo(tempStream);
        Buffer = tempStream.GetBuffer();
    }

    public override void Serialize(BinaryWriter writer)
    {
        writer.Write(Buffer);
    }
}