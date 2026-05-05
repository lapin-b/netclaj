using System.Runtime.InteropServices;
using NetClajServer.Datastructures;
using NetClajServer.Packets;

namespace NetClajServer.Mindustry;

public class Serializer
{
    public byte[] Serialize(IMindustryPacket packet)
    {
        var memoryStream = new MemoryStream(1024);
        var binaryWriter = new BinaryWriter(memoryStream);

        // Leave two symbols for the packet size calculated at the end
        binaryWriter.Seek(2, SeekOrigin.Begin);
        
        // Write the packet header: wrapper packet type, inner packet type
        binaryWriter.Write(packet.GetPacketType());
        binaryWriter.Write(packet.GetPacketIdentifier());
        
        packet.Serialize(binaryWriter);
        
        // The cursor is at the end. The length of this packet is n - 2
        var packetLength = memoryStream.Position - 2;
        binaryWriter.Seek(0, SeekOrigin.Begin);
        binaryWriter.WriteInt16BigEndian((short)packetLength);

        return memoryStream.ToArray();
    }

    public IMindustryPacket Deserialize(ReadOnlyMemory<byte> payload)
    {
        Stream stream;
        if (MemoryMarshal.TryGetArray(payload, out var segment) && segment.Array is not null)
        {
            stream = new MemoryStream(
                segment.Array,
                segment.Offset,
                segment.Count,
                false,
                true
            );
        }
        else
        {
            stream = new MemoryStream(payload.ToArray(), writable: false);
        }
        
        var binaryReader = new BinaryReader(stream);

        _ = binaryReader.ReadInt16BigEndian(); // We don't care about the size of the packet
        var packetType = binaryReader.ReadSByte();

        IMindustryPacket? packet = null;
        
        switch (packetType)
        {
            case PacketType.Framework:
                return DecodeFrameworkPacket(binaryReader);
            
            default:
                throw new NotImplementedException();
        }

        throw new NotImplementedException();
    }

    private IMindustryPacket DecodeFrameworkPacket(BinaryReader reader)
    {
        
        var packetType = reader.ReadByte();

        IMindustryPacket deserializedPacket = packetType switch
        {
            RegisterTcpPacket.Identifier => new RegisterTcpPacket(),
            RegisterUdpPacket.Identifier => new RegisterUdpPacket(),
            _ => throw new ArgumentOutOfRangeException(nameof(packetType))
        };

        deserializedPacket.Deserialize(reader);
        return deserializedPacket;
    }
}