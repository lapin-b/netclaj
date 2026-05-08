using System.Runtime.InteropServices;
using NetClajServer.Datastructures;
using NetClajServer.Packets;
using NetClajServer.Packets.Claj;
using NetClajServer.Packets.Framework;

namespace NetClajServer.Mindustry;

public static class Serializer
{
    public static byte[] Serialize(IMindustryPacket packet, bool isTcp = true)
    {
        var memoryStream = new MemoryStream(1024);
        var binaryWriter = new BinaryWriter(memoryStream);

        // Leave two symbols for the packet size calculated at the end
        if (isTcp)
        {
            binaryWriter.Seek(2, SeekOrigin.Begin);
        }
        
        // Write the packet header: wrapper packet type, inner packet type
        if (packet is not RawPacket)
        {
            binaryWriter.Write(packet.GetPacketType());
            binaryWriter.Write(packet.GetPacketIdentifier());
        }

        packet.Serialize(binaryWriter);

        if (isTcp)
        {
            // The cursor is at the end. The length of this packet is n - 2
            var packetLength = memoryStream.Position - 2;
            binaryWriter.Seek(0, SeekOrigin.Begin);
            binaryWriter.WriteInt16BigEndian((short)packetLength);   
        }

        return memoryStream.ToArray();
    }

    public static IMindustryPacket Deserialize(ReadOnlyMemory<byte> payload)
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
                var packet = new RawPacket();
                packet.Deserialize(binaryReader);
                return packet;
        }
    }

    private static IMindustryPacket DecodeClajPacket(BinaryReader reader)
    {
        var packetType = reader.ReadByte();

        IMindustryPacket deserializedPacket = packetType switch
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

    private static IMindustryPacket DecodeFrameworkPacket(BinaryReader reader)
    {
        
        var packetType = reader.ReadByte();

        IMindustryPacket deserializedPacket = packetType switch
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