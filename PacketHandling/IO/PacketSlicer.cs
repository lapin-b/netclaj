using System.Buffers;
using System.Buffers.Binary;

namespace PacketHandling.IO;

public static class PacketSlicer
{
    public static bool TryReadFrame(
        ref ReadOnlySequence<byte> buffer,
        out ReadOnlySequence<byte> payload)
    {
        payload = default;

        /*
         * A valid TCP Mindustry frame contains a 2-bytes preamble indicating how long
         * is the payload to come (a ushort) + n bytes of payload.
         *
         * The buffer must have enough bytes to extract a valid frame
         */
        if (buffer.Length < 2)
        {
            return false;
        }

        // Read the next packet length; it fits in an ushort

        ushort packetLength;
        var lengthSlice = buffer.Slice(0, 2);
        if (lengthSlice.IsSingleSegment)
        {
            var segment = lengthSlice.First.Span;
            packetLength = BinaryPrimitives.ReadUInt16BigEndian(segment);
        }
        else
        {
            Span<byte> packetLengthBytes = stackalloc byte[2];
            buffer.Slice(0, 2).CopyTo(packetLengthBytes);
            packetLength = BinaryPrimitives.ReadUInt16BigEndian(packetLengthBytes);
        }
        
        long frameSize = 2 + packetLength;
        if (buffer.Length < frameSize)
        {
            // Buffered network traffic is not enough for extract a valid frame
            return false;
        }

        // Extract a frame while cutting off the length. This allows the packet processing to be agnostic
        // of the transport of the original packet.
        
        // `payload` contains the packet minus the length prefix
        payload = buffer.Slice(2, packetLength);
        
        // The beginning of `buffer` now points to the first byte of unread data. The unread stuff can be
        // anything, from a complete packet that will be extracted on the next loop to incomplete
        // stuff that needs more buffering.
        buffer = buffer.Slice(frameSize);

        return true;
    }
}