namespace NetClajServer.Packets.Claj;

public class ClajPayloadWrapping: MindustryPacketWithConId
{
    public bool IsTcp { get; set; }
    public byte[] Buffer { get; set; } = [];
    
    public const byte Identifier = 0;
    public override byte GetPacketIdentifier() => Identifier;

    protected override void DeserializeInnerPayload(BinaryReader reader)
    {
        IsTcp = reader.ReadBoolean();

        var buffer = new MemoryStream((int)reader.BaseStream.Length - 1);
        reader.BaseStream.Seek(1, SeekOrigin.Begin);
        reader.BaseStream.CopyTo(buffer);
        Buffer = buffer.GetBuffer();
    }

    protected override void SerializeInnerPayload(BinaryWriter writer)
    {
        writer.Write(IsTcp);
        writer.Write(Buffer);
    }
}