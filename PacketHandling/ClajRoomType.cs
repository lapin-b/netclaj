using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;
using PacketHandling.IO;

namespace PacketHandling;

public struct ClajRoomType
{
    public static readonly ClajRoomType Empty = new();
    
    public string Type { get; set; } = string.Empty;

    public ClajRoomType()
    {
    }

    public static ClajRoomType FromPacketReader(ref PacketReader reader)
    {
        ushort roomTypeLength = reader.ReadByte("Room type length")
            .Ensure(l => l <= 16, PacketErrorCode.LimitExceeded, "Room type length is too long");
        
        string roomType = reader.ReadExactBytes("Room type", roomTypeLength)
            .Map(seq => Encoding.ASCII.GetString(seq));
        
        return new ClajRoomType { Type = roomType };
    }
    
    public static implicit operator string (ClajRoomType roomType) => roomType.Type;

    public void Serialize(IBufferWriter<byte> writer)
    {
        writer.WriteIntegerBe((byte)Type.Length);
        writer.Write(MemoryMarshal.AsBytes(Type.AsSpan()));
    }
}