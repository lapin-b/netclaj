using System.Buffers;
using PacketHandling.IO;

namespace PacketHandling;

/// <summary>
/// Base packet of the binary protocol processed by a CLaJ node
/// </summary>
public abstract class MindustryPacket
{
    /// <summary>
    /// Indicate if the packet arrived on the TCP transport or not. True for TCP, false for UDP
    /// </summary>
    public bool TransportIsTcp { get; set; } = true;
    
    /// <summary>
    /// Get the family of this packet. See <see cref="PacketType"/>.
    /// </summary>
    /// <returns>The packet family as a signed byte</returns>
    public abstract sbyte GetPacketFamily();
    
    /// <summary>
    /// Get the identifier of this packet in the family.
    /// </summary>
    /// <returns>The packet identifier in the packet family</returns>
    public abstract byte GetPacketIdentifier();
    
    /// <summary>
    /// Read bytes from the <paramref name="reader"/> into object attributes.
    /// </summary>
    /// <remarks>
    /// The packet family and identifier when applicable have already been read. The reader is
    /// pointing at the first byte to decode.
    /// </remarks>
    /// <param name="reader">The binary reader to decode the packet with</param>
    public abstract PacketResult TryDeserialize(ref PacketReader reader);

    /// <summary>
    /// Write object attributes into a sequence of bytes. The packet family and identifier have already been
    /// written into the buffer.
    /// </summary>
    /// <param name="writer">The binary writer to write the packet with</param>
    public abstract void Serialize(IBufferWriter<byte> writer);
}