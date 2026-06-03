using NetClajServer.Claj;
using NetClajServer.Datastructures;
using NetClajServer.Packets.IO;

namespace NetClajServer.Packets.Claj;

public class RoomClosedPacket: MindustryPacket
{
    public const sbyte Type = PacketType.Claj;
    public const byte Identifier = 6;
    public ClajConnectionCloseReason Reason { get; set; }
    
    public override sbyte GetPacketFamily() => Type;

    public override byte GetPacketIdentifier() => Identifier;

    public override void Serialize(BinaryWriter writer)
    {
        writer.WriteInt32BigEndian((int)Reason);
    }

    public override PacketResult TryDeserialize(ref PacketReader reader)
    {
        Reason = (ClajConnectionCloseReason)reader.ReadIntBigEndian(nameof(Reason)).Value;
        return reader.Result;
    }
}