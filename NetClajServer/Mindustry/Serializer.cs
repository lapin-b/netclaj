using System.Buffers;
using NetClajServer.Datastructures;
using NetClajServer.Packets;
using NetClajServer.Packets.Claj;
using NetClajServer.Packets.Framework;
using NetClajServer.Packets.IO;
using NetClajServer.Packets.Streaming;

namespace NetClajServer.Mindustry;

public static class Serializer
{
    public static ReadOnlyMemory<byte> Serialize(MindustryPacket packet, MemoryStream memoryStream, BinaryWriter binaryWriter, bool isTcp = true)
    {
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

    public static ReadOnlyMemory<byte> Serialize(List<MindustryPacket> packets, MemoryStream memoryStream, BinaryWriter binaryWriter, bool isTcp = true)
    {
        foreach (var packet in packets)
        {
            var packetStartPosition = (int)memoryStream.Position;
            memoryStream.Seek(2, SeekOrigin.Current);
            
            if (packet is not GamePacket)
            {
                binaryWriter.Write(packet.GetPacketFamily());
                binaryWriter.Write(packet.GetPacketIdentifier());
            }
            
            packet.Serialize(binaryWriter);

            if (isTcp)
            {
                var packetEndPosition = (int)memoryStream.Position;
                var packetLength = (int)(memoryStream.Position - packetStartPosition - 2);
                binaryWriter.Seek(packetStartPosition, SeekOrigin.Begin);
                binaryWriter.WriteInt16BigEndian((short)packetLength);
                binaryWriter.Seek(packetEndPosition, SeekOrigin.Begin);
            }
        }
        
        return memoryStream.GetBuffer().AsMemory(0, (int)memoryStream.Length);
    }

    public static MindustryPacket Deserialize(ReadOnlyMemory<byte> payload)
    {
        return Deserialize(new ReadOnlySequence<byte>(payload));
    }
    
    public static MindustryPacket Deserialize(ReadOnlySequence<byte> payload)
    {
        var reader = new SequenceReader<byte>(payload);

        // The stream to deserialize has already its packet size cut off if it was TCP. The next byte contains the nature
        // of the payload to come.
        if (!reader.TryRead(out var packetTypeAsByte))
        {
            throw new EndOfStreamException("Missing packet family byte in payload");
        }

        var packetType = (sbyte)packetTypeAsByte;

        MindustryPacket packetToDeserialize;
        switch (packetType)
        {
            case PacketType.Framework:
                packetToDeserialize = DecodeFrameworkPacket(ref reader);
                break;
            case PacketType.OldClajVersion:
                throw new InvalidOperationException("Old claj client detected");
            case PacketType.Claj:
                packetToDeserialize = DecodeClajPacket(ref reader);
                break;
            default:
                // If the payload nature can't be recognized, it must be a "raw" Mindustry packet, that is one that will
                // eventually be relayed.
                reader.Rewind(1);
                packetToDeserialize = new GamePacket
                {
                    Buffer = reader.UnreadSequence
                };

                reader.Advance(reader.Remaining);
                return packetToDeserialize;
        }
        
        var packetReader = new PacketReader(reader.UnreadSequence);
        var outcome = packetToDeserialize.TryDeserialize(ref packetReader);

        if (outcome.IsFailure)
        {
            throw new SerializerException(
                $"Error while parsing {outcome.PacketName} field {outcome.Field}: {outcome.Code} on byte {outcome.Offset}: {outcome.Detail ?? "<no further details>"}"
            );
        }
        
        reader.Advance(reader.Remaining);
        
        return packetToDeserialize;
        
    }

    private static MindustryPacket DecodeClajPacket(ref SequenceReader<byte> reader)
    {
        if (!reader.TryRead(out var packetType))
        {
            throw new EndOfStreamException("Missing CLaJ packet identifier");
        }

        MindustryPacket deserializedPacket = packetType switch
        {
            ClajMessagePacket.Identifier => new ClajMessagePacket(),
            ClajPayloadWrapping.Identifier => new ClajPayloadWrapping(),
            ClajPopupPacket.Identifier => new ClajPopupPacket(),
            ClajTextMessagePacket.Identifier => new ClajTextMessagePacket(),
            ConnectionClosedPacket.Identifier => new ConnectionClosedPacket(),
            ConnectionIdlingPacket.Identifier => new ConnectionIdlingPacket(),
            ConnectionJoinPacket.Identifier => new ConnectionJoinPacket(),
            RoomClosedPacket.Identifier => new RoomClosedPacket(),
            RoomClosureRequestPacket.Identifier => new RoomClosureRequestPacket(),
            RoomConfigPacket.Identifier => new RoomConfigPacket(),
            RoomCreationRequestPacket.Identifier => new RoomCreationRequestPacket(),
            RoomInfoDeniedPacket.Identifier => new RoomInfoDeniedPacket(),
            RoomInfoPacket.Identifier => new RoomInfoPacket(),
            RoomInfoRequestPacket.Identifier => new RoomInfoRequestPacket(),
            RoomJoinAcceptedPacket.Identifier => new RoomJoinAcceptedPacket(),
            RoomJoinDeniedPacket.Identifier => new RoomJoinDeniedPacket(),
            RoomJoinPacket.Identifier => new RoomJoinPacket(),
            RoomJoinRequestPacket.Identifier => new RoomJoinRequestPacket(),
            RoomLinkPacket.Identifier => new RoomLinkPacket(),
            RoomListPacket.Identifier => new RoomListPacket(),
            RoomListRequestPacket.Identifier => new RoomListRequestPacket(),
            RoomStatePacket.Identifier => new RoomStatePacket(),
            RoomStateRequestPacket.Identifier => new RoomStateRequestPacket(),
            ServerInfoPacket.Identifier => new ServerInfoPacket(),
            StreamChunk.Identifier => new StreamChunk(),
            StreamHead.Identifier => new StreamHead(),
            _ => throw new SerializerException(nameof(packetType), SerializerException.FamilyClaj, packetType)
        };
        
        return deserializedPacket;
    }

    private static MindustryPacket DecodeFrameworkPacket(ref SequenceReader<byte> reader)
    {
        
        if (!reader.TryRead(out var packetType))
        {
            throw new EndOfStreamException("Missing arc.net Framework packet identifier");
        }

        MindustryPacket deserializedPacket = packetType switch
        {
            PingPacket.Identifier => new PingPacket(),
            DiscoverHostPacket.Identifier => new DiscoverHostPacket(),
            KeepAlivePacket.Identifier => new KeepAlivePacket(),
            RegisterUdpPacket.Identifier => new RegisterUdpPacket(),
            RegisterTcpPacket.Identifier => new RegisterTcpPacket(),
            _ => throw new SerializerException(nameof(packetType), SerializerException.FamilyFramework, packetType)
        };

        return deserializedPacket;
    }
}