using System.Runtime.InteropServices;
using NetClajServer.Datastructures;
using NetClajServer.Packets;
using NetClajServer.Packets.Claj;
using NetClajServer.Packets.Framework;

namespace NetClajServer.Mindustry;

public static class Serializer
{
    public static Memory<byte> Serialize(MindustryPacket packet, bool isTcp = true)
    {
        var memoryStream = new MemoryStream(1024);
        var binaryWriter = new BinaryWriter(memoryStream);

        // If the packet is sent over TCP, we should skip two bytes that will later receive
        // the payload length of this packet. UDP doesn't care about that.
        if (isTcp)
        {
            binaryWriter.Seek(2, SeekOrigin.Begin);
        }
        
        if (packet is not GamePacket)
        {
            binaryWriter.Write(packet.GetPacketFamily());
            binaryWriter.Write(packet.GetPacketIdentifier());
        }

        packet.Serialize(binaryWriter);

        if (isTcp)
        {
            // The cursor is at the end. The payload length of the packet is position - 2
            var packetLength = memoryStream.Position - 2;
            binaryWriter.Seek(0, SeekOrigin.Begin);
            binaryWriter.WriteInt16BigEndian((short)packetLength);   
        }

        return memoryStream.GetBuffer().AsMemory(0, (int)memoryStream.Length);
    }
    
    public static MindustryPacket Deserialize(ReadOnlyMemory<byte> payload)
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
        
        // The stream to deserialize has already its packet size cut off if it was TCP. The next byte contains the nature
        // of the payload to come.
        var binaryReader = new BinaryReader(stream);
        var packetType = binaryReader.ReadSByte();

        switch (packetType)
        {
            case PacketType.Framework:
                return DecodeFrameworkPacket(binaryReader);
            case PacketType.OldClajVersion:
                throw new NotImplementedException();
            case PacketType.Claj:
                return DecodeClajPacket(binaryReader);
            default:
                // If the payload nature can't be recognized, it must be a "raw" Mindustry packet, that is one that will
                // eventually be relayed.
                
                // Seek to zero since the first two bytes were cut off from the payload in TCP transport
                binaryReader.BaseStream.Seek(0, SeekOrigin.Begin);
                var packet = new GamePacket();
                packet.Deserialize(binaryReader);
                return packet;
        }
    }

    private static MindustryPacket DecodeClajPacket(BinaryReader reader)
    {
        var packetType = reader.ReadByte();

        MindustryPacket deserializedPacket = packetType switch
        {
            ClajPayloadWrapping.Identifier => new ClajPayloadWrapping(),
            ConnectionClosedPacket.Identifier => new ConnectionClosedPacket(),
            ConnectionJoinPacket.Identifier => new ConnectionJoinPacket(),
            ConnectionIdlingPacket.Identifier => new ConnectionIdlingPacket(),
            RoomCreateRequestPacket.Identifier => new RoomCreateRequestPacket(),
            RoomCloseRequestPacket.Identifier => new RoomCloseRequestPacket(),
            RoomLinkPacket.Identifier => new RoomLinkPacket(),
            RoomJoinPacket.Identifier => new RoomJoinPacket(),
            ClajMessagePacket.Identifier => new ClajMessagePacket(),
            _ => throw new SerializerException(nameof(packetType), SerializerException.FamilyClaj, packetType)
        };
        
        deserializedPacket.Deserialize(reader);
        return deserializedPacket;
    }

    private static MindustryPacket DecodeFrameworkPacket(BinaryReader reader)
    {
        
        var packetType = reader.ReadByte();

        MindustryPacket deserializedPacket = packetType switch
        {
            PingPacket.Identifier => new PingPacket(),
            DiscoverHostPacket.Identifier => new DiscoverHostPacket(),
            KeepAlivePacket.Identifier => new KeepAlivePacket(),
            RegisterUdpPacket.Identifier => new RegisterUdpPacket(),
            RegisterTcpPacket.Identifier => new RegisterTcpPacket(),
            _ => throw new SerializerException(nameof(packetType), SerializerException.FamilyFramework, packetType)
        };

        deserializedPacket.Deserialize(reader);
        return deserializedPacket;
    }
}