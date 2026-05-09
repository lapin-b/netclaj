using NetClajServer.Datastructures;

namespace NetClajServer.Packets.Framework;

public class GamePacket: MindustryPacket
{
    public byte[] Buffer { get; set; } = [];

    public GamePacket()
    {
        
    }

    public GamePacket(byte[] buffer)
    {
        Buffer = buffer;
    }

    public override sbyte GetPacketType() => sbyte.MaxValue;
    public override byte GetPacketIdentifier() => byte.MaxValue;

    public override void Deserialize(BinaryReader reader)
    {
        Buffer = reader.BaseStream.ReadToByteArray();
    }

    public override void Serialize(BinaryWriter writer)
    {
        writer.Write(Buffer);
    }
}