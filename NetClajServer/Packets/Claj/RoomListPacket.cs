using System.Buffers.Binary;
using NetClajServer.Claj;
using NetClajServer.Packets.IO;
using NetClajServer.Packets.Streaming;

namespace NetClajServer.Packets.Claj;

public class RoomListPacket: MindustryPacket, ISequenceDeserializable, IStreamablePacket
{
    public const sbyte Type = PacketType.Claj;
    public const byte Identifier = 19;

    public List<Room> Rooms { get; set; } = [];
    
    public override sbyte GetPacketFamily() => Type;

    public override byte GetPacketIdentifier() => Identifier;

    public override void Deserialize(BinaryReader reader)
    {
        throw new NotSupportedException();
    }
    
    public PacketResult TryDeserialize(ref PacketReader reader)
    {
        throw new NotSupportedException();
    }

    public override void Serialize(BinaryWriter writer)
    {
        throw new NotImplementedException();
    }

    public int StreamTotalPacketSize()
    {
        var roomsPayloadLength = Rooms.Sum(RoomPacketSize);
        // Rooms count + payload length
        return sizeof(long) + roomsPayloadLength;
    }
   
    public async Task StreamChunks(TcpStreamSink sink)
    {
        var b1 = new byte[1];
        var i16 = new byte[2];
        var i32 = new byte[4];
        var i64 = new byte[8];

        BinaryPrimitives.WriteInt32BigEndian(i32, Rooms.Count);
        await sink.Write(i32);

        foreach (var room in Rooms)
        {
            BinaryPrimitives.WriteInt64BigEndian(i64, room.Id);
            await sink.Write(i64);

            b1[0] = (byte)(room.Configuration.IsProtectedByPin ? 1 : 0);
            await sink.Write(b1);
            
            BinaryPrimitives.WriteInt16BigEndian(i16, (short)room.State.Length);
            await sink.Write(i16);

            await sink.Write(room.State);
        }
    }

    // Room id + is protected flag + state length + state itself
    private static int RoomPacketSize(Room room) => sizeof(long) + sizeof(bool) + sizeof(ushort) + room.State.Length;
}