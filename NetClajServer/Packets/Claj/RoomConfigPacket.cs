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
        const string packetName = nameof(RoomConfigPacket);
        reader.NeedByte(packetName, "RoomConfig", out var config);
        reader.NeedShortBigEndian(packetName, nameof(Pin), out var pin);
        reader.NeedShortBigEndian(packetName, nameof(MaxClients), out var maxClients);
        if (reader.Result.IsFailure) return reader.Result;

        IsPublic =            (config & 0b0100) == 0b0100;
        IsProtectedByPin =    (config & 0b0010) == 0b0010;
        CanRequestHostState = (config & 0b0001) == 0b0001;

        Pin = pin;
        MaxClients = maxClients;

        return PacketResult.Ok();
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