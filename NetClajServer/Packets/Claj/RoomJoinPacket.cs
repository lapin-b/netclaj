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
        const string packetName = nameof(RoomJoinRequestPacket);
        bool withPin;
        short? pin;
        ReadOnlySequence<byte> roomTypeBytes;
        
        reader.NeedLongBigEndian(packetName, nameof(RoomId), out var roomId);
        
        // Compatibility with older versions of the client. This will only work if the room doesn't have a pin set.
        if (reader.Remaining > 0)
        {
            reader.NeedBoolean(packetName, nameof(WithPin), out withPin);
            reader.NeedShortBigEndian(packetName, nameof(Pin), out var tempPin);
            reader.NeedByte(packetName, "room type length", out var roomLength);
            reader.Require(roomLength < 16, packetName, nameof(RoomType), PacketErrorCode.LimitExceeded, $"Was to read {roomLength} bytes");
            reader.NeedReadExact(packetName, nameof(RoomType), roomLength, out roomTypeBytes);

            pin = tempPin;
        }
        else
        {
            withPin = false;
            pin = null;
            roomTypeBytes = ReadOnlySequence<byte>.Empty;
        }

        if (reader.ProcessingFailed) return reader.Result;

        RoomId = roomId;
        WithPin = withPin;
        Pin = pin;
        RoomType = Encoding.ASCII.GetString(roomTypeBytes);

        return PacketResult.Ok();
    }
}