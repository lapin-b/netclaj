using NetClajServer.Claj;
using NetClajServer.Datastructures;
using NetClajServer.Packets.IO;

namespace NetClajServer.Packets.Claj;

public class RoomConfigPacket: MindustryPacket, ISequenceDeserializable
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

    public override void Deserialize(BinaryReader reader)
    {
        throw new NotSupportedException();
    }

    public override void Serialize(BinaryWriter writer)
    {
        var configFlags =
            (IsPublic ? 1 : 0) << 2
            | (IsProtectedByPin ? 1 : 0) << 1
            | (CanRequestHostState ? 1 : 0);

        var config = (byte)configFlags;
        
        writer.Write(config);
        writer.WriteInt16BigEndian(Pin ?? -1);
        writer.WriteInt16BigEndian(MaxClients);
    }

    public PacketResult TryDeserialize(ref PacketReader reader)
    {
        reader.WithPacketName(nameof(RoomConfigPacket));
        var config = reader.NeedByte("RoomConfig").Value;
        Pin = reader.NeedShortBigEndian(nameof(Pin));
        MaxClients = reader.NeedShortBigEndian(nameof(MaxClients));

        IsPublic =            (config & 0b0100) == 0b0100;
        IsProtectedByPin =    (config & 0b0010) == 0b0010;
        CanRequestHostState = (config & 0b0001) == 0b0001;
        
        return reader.Result;
    }

    public RoomConfiguration IntoRoomConfiguration()
    {
        return new RoomConfiguration
        {
            IsPublic = IsPublic,
            IsProtectedByPin = IsProtectedByPin,
            CanRequestHostState = CanRequestHostState,
            Pin = Pin,
            MaxClients = MaxClients
        };
    }
}