using System.Buffers;
using PacketHandling.IO;
using PacketHandling.Serialization;

namespace PacketHandling.Claj;

public class RoomConfigPacket: MindustryPacket
{
    public const sbyte Type = PacketType.Claj;
    public const byte Identifier = 12;
    
    public bool IsPublic { get; set; }
    public bool IsProtectedByPin { get; set; }
    public bool CanRequestHostState { get; set; }
    
    public short? Pin { get; set; }
    public short MaxClients { get; set; }
    
    public override sbyte GetPacketFamily() => Type;

    public override byte GetPacketIdentifier() => Identifier;

    public override void Serialize(IBufferWriter<byte> writer)
    {
        var configFlags =
            ((IsPublic ? 1 : 0) << 2)
            | ((IsProtectedByPin ? 1 : 0) << 1)
            | (CanRequestHostState ? 1 : 0);

        var config = (byte)configFlags;
        
        writer.WriteIntegerBe(config);
        writer.WriteIntegerBe(Pin ?? -1);
        writer.WriteIntegerBe(MaxClients);
    }

    public override PacketResult TryDeserialize(ref PacketReader reader)
    {
        reader.WithPacketName(nameof(RoomConfigPacket));
        var config = reader.ReadByte("RoomConfig").Value;
        Pin = reader.ReadShortBigEndian(nameof(Pin));
        MaxClients = reader.ReadShortBigEndian(nameof(MaxClients));

        IsPublic =            (config & 0b0100) == 0b0100;
        IsProtectedByPin =    (config & 0b0010) == 0b0010;
        CanRequestHostState = (config & 0b0001) == 0b0001;
        
        return reader.Result;
    }
}