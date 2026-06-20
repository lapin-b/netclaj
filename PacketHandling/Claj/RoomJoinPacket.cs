using System.Buffers;
using System.Text;
using PacketHandling.IO;

namespace PacketHandling.Claj;

public class RoomJoinPacket: MindustryPacket
{
    public long RoomId { get; set; }
    public bool WithPin { get; set; }
    public short? Pin { get; set; }
    public string RoomType { get; set; } = string.Empty;
    
    public const sbyte Type = PacketType.Claj;
    public const byte Identifier = 7;

    public override sbyte GetPacketFamily() => Type;
    public override byte GetPacketIdentifier() => Identifier;

    public override void Serialize(IBufferWriter<byte> writer)
    {
        writer.WriteIntegerBe(RoomId);
    }

    public override PacketResult TryDeserialize(ref PacketReader reader)
    {
        reader.WithPacketName(nameof(RoomJoinRequestPacket));

        RoomId = reader.ReadRoomId(nameof(RoomId));
        
        // Compatibility with older versions of the client. This will only work if the room doesn't have a pin set.
        if (reader.Remaining > 0)
        {
            WithPin = reader.ReadBoolean(nameof(WithPin));
            Pin = reader.ReadShortBigEndian(nameof(Pin));
            
            var roomLength = reader
                .ReadByte("room type length")
                .Ensure(l => l <= 16, PacketErrorCode.LimitExceeded, "Room type length is too long");

            RoomType = reader.ReadExactBytes(nameof(RoomType), roomLength)
                .Map(seq => Encoding.ASCII.GetString(seq));
        }
        else
        {
            WithPin = false;
            Pin = null;
            RoomType = string.Empty;
        }

        return reader.ProcessingFailed ? reader.Result : PacketResult.Ok();
    }
}