using System.Buffers;
using PacketHandling.IO;
using PacketHandling.Support;

namespace PacketHandling.Claj;

public class RoomListRequestPacket: MindustryPacket
{
    public const sbyte Type = PacketType.Claj;
    public const byte Identifier = 18;

    public ClajRoomType RequestedRoomType { get; set; } = ClajRoomType.Empty;
    
    public override sbyte GetPacketFamily() => Type;

    public override byte GetPacketIdentifier() => Identifier;

    public override void Serialize(IBufferWriter<byte> writer)
    {
        
    }

    public override PacketResult TryDeserialize(ref PacketReader reader)
    {
        RequestedRoomType = ClajRoomType.FromPacketReader(ref reader);
        return reader.Result;
    }
}