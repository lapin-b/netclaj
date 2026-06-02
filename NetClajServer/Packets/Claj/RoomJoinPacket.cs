using System.Buffers;
using System.Text;
using NetClajServer.Datastructures;
using NetClajServer.Packets.IO;

namespace NetClajServer.Packets.Claj;

public class RoomJoinPacket: MindustryPacket, ISequenceDeserializable
{
    public long RoomId { get; set; }
    public bool WithPin { get; set; }
    public short? Pin { get; set; }
    public string RoomType { get; set; } = string.Empty;
    
    public const sbyte Type = PacketType.Claj;
    public const byte Identifier = 7;

    public override sbyte GetPacketFamily() => Type;
    public override byte GetPacketIdentifier() => Identifier;
    
    public override void Deserialize(BinaryReader reader)
    {
        RoomId = reader.ReadInt64BigEndian();
    }

    public override void Serialize(BinaryWriter writer)
    {
        writer.WriteInt64BigEndian(RoomId);
    }

    public PacketResult TryDeserialize(ref PacketReader reader)
    {
        reader.WithPacketName(nameof(RoomJoinRequestPacket));

        RoomId = reader.NeedRoomId(nameof(RoomId));
        
        // Compatibility with older versions of the client. This will only work if the room doesn't have a pin set.
        if (reader.Remaining > 0)
        {
            WithPin = reader.NeedBoolean(nameof(WithPin));
            Pin = reader.NeedShortBigEndian(nameof(Pin));
            
            var roomLength = reader
                .NeedByte("room type length")
                .Ensure(l => l <= 16, PacketErrorCode.LimitExceeded, "Room type length is too long");

            RoomType = reader.NeedReadExact(nameof(RoomType), roomLength)
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