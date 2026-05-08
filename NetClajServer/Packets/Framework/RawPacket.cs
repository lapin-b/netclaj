namespace NetClajServer.Packets.Framework;

public class RawPacket: IMindustryPacket
{
    public byte[] Buffer { get; set; } = [];

    public RawPacket()
    {
        
    }

    public RawPacket(MemoryStream buffer)
    {
        buffer.Seek(0, SeekOrigin.Begin);
        Buffer = buffer.GetBuffer();
    }

    public RawPacket(byte[] buffer)
    {
        Buffer = buffer;
    }

    public sbyte GetPacketType() => sbyte.MaxValue;
    public byte GetPacketIdentifier() => byte.MaxValue;

    public void Deserialize(BinaryReader reader)
    {
        var tempStream = new MemoryStream();
        reader.BaseStream.CopyTo(tempStream);
        Buffer = tempStream.GetBuffer();
    }

    public void Serialize(BinaryWriter writer)
    {
        writer.Write(Buffer);
    }
}