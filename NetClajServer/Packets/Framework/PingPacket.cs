using System.Buffers;
using NetClajServer.Datastructures;
using NetClajServer.Packets.IO;

namespace NetClajServer.Packets.Framework;

public class PingPacket: MindustryPacket, ISequenceDeserializable
{
    public const sbyte Type = PacketType.Framework;
    public const byte Identifier = 0;

    public int Id { get; set; }
    public bool IsReply { get; set; }
    
    public override sbyte GetPacketFamily() => Type;
    public override byte GetPacketIdentifier() => Identifier;
    public override void Deserialize(BinaryReader reader)
    {
        Id = reader.ReadInt32BigEndian();
        IsReply = reader.ReadBoolean();
    }
    
    public PacketResult TryDeserialize(ref PacketReader reader)
    {
        const string packetName = nameof(PingPacket);

        if (
            !reader.TryReadIntBigEndian(packetName, nameof(Id), out int pingId, out var err) 
            || !reader.TryReadBoolean(packetName, nameof(IsReply), out var isReply, out err)
        ) return err;

        Id = pingId;
        IsReply = isReply;
        
        return PacketResult.Ok();
    }

    public override void Serialize(BinaryWriter writer)
    {
        writer.WriteInt32BigEndian(Id);
        writer.Write(IsReply);
    }
}