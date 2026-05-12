using NetClajServer.Datastructures;

namespace NetClajServer.Packets.Claj;

public class ClajPayloadWrapping: MindustryPacketWithConId
{
    public bool WrappedPacketIsTcp { get; set; }
    public byte[] Buffer { get; set; } = [];
    
    public const byte Identifier = 2;
    public override byte GetPacketIdentifier() => Identifier;

    protected override void DeserializeInnerPayload(BinaryReader reader)
    {
        WrappedPacketIsTcp = reader.ReadBoolean();
        Buffer = reader.BaseStream.ReadToByteArray();
    }

    protected override void SerializeInnerPayload(BinaryWriter writer)
    {
        writer.Write(WrappedPacketIsTcp);
        writer.Write(Buffer);
    }
}