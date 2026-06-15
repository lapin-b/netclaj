using System.Buffers;
using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.InteropServices;

namespace NetClajServer.Packets.IO;

/// <summary>
/// Helper to deserialize Mindustry packets from a <see cref="ReadOnlySequence{T}"/>.
/// </summary>
public class PacketReader
{
    public long Consumed { get; private set; }
    public long Remaining => _sequence.Length - Consumed;
    public bool ProcessingFailed => Result.IsFailure;

    private readonly ReadOnlySequence<byte> _sequence;
    private string _packetName = "(unknown packet)";
    
    // Keep the error state internally and skip deserializing the packet if the error is set
    public PacketResult Result = PacketResult.Ok();

    public PacketReader(ReadOnlySequence<byte> sequence)
    {
        _sequence = sequence;
        Consumed = 0;
    }

    public void WithPacketName(string packetName)
    {
        _packetName = packetName;
    }
    
    public PacketIntermediateProcessing<byte> ReadByte(string field)
    {
        if (ProcessingFailed) return new PacketIntermediateProcessing<byte>(0, _packetName, field, this);
        if(Remaining < 1) return FailEof<byte>(_packetName, field);

        var value = _sequence.Slice(Consumed, 1).FirstSpan[0];
        Consumed++;
        return new PacketIntermediateProcessing<byte>(value, _packetName, field, this);
    }

    public PacketIntermediateProcessing<short> ReadShortBigEndian(string field)
    {
        return ImplReadInteger(field, BinaryPrimitives.ReadInt16BigEndian);
    }
    
    public PacketIntermediateProcessing<ushort> ReadUShortBigEndian(string field)
    {
        return ImplReadInteger(field, BinaryPrimitives.ReadUInt16BigEndian);
    }
    
    public PacketIntermediateProcessing<int> ReadIntBigEndian(string field)
    {
        return ImplReadInteger(field, BinaryPrimitives.ReadInt32BigEndian);
    }
    
    public PacketIntermediateProcessing<long> ReadLongBigEndian(string field)
    {
        return ImplReadInteger(field, BinaryPrimitives.ReadInt64BigEndian);
    }

    public PacketIntermediateProcessing<long> ReadRoomId(string field)
    {
        return ReadLongBigEndian(field);
    }

    public PacketIntermediateProcessing<int> ReadConnectionId(string field)
    {
        return ReadIntBigEndian(field);
    }

    public PacketIntermediateProcessing<bool> ReadBoolean(string field)
    {
        return ReadByte(field).Map(b => b != 0);
    }

    public PacketIntermediateProcessing<ReadOnlySequence<byte>> ReadExactBytes(string field, int count)
    {
        if (ProcessingFailed) return new PacketIntermediateProcessing<ReadOnlySequence<byte>>(default, _packetName, field, this);
        if(Remaining < count) return FailEof<ReadOnlySequence<byte>>(_packetName, field);
        
        var bytes = _sequence.Slice(Consumed, count);
        Consumed += count;
        return new PacketIntermediateProcessing<ReadOnlySequence<byte>>(bytes, _packetName, field, this);
    }

    public PacketIntermediateProcessing<ReadOnlySequence<byte>> ReadRest()
    {
        if (ProcessingFailed) return new(default, _packetName, "(rest of sequence)", this);
        
        var bytesCount = Remaining;
        var slice = _sequence.Slice(Consumed, bytesCount);
        Consumed += bytesCount;

        return new PacketIntermediateProcessing<ReadOnlySequence<byte>>(
            slice, _packetName, "(rest of sequence)", this
        );
    }

    public PacketIntermediateProcessing<string> ReadJavaUtf(string field)
    {
        // Copy-pasted Java implementation, adapted to the fluent interface of the packet reader
        var utflen = ReadUShortBigEndian(field).Value;
        var bytearr = ReadExactBytes(field, utflen)
            .Map(seq =>
            {
                if(seq.IsSingleSegment) return seq.First;
                return seq.ToArray().AsMemory();
            })
            .Value
            .Span;

        if(ProcessingFailed) return new PacketIntermediateProcessing<string>(string.Empty, _packetName, field, this);

        try
        {
            var theString = JavaDataObjectStream.ReadUtf(bytearr, utflen);
            return new PacketIntermediateProcessing<string>(theString, _packetName, field, this);
        }
        catch (UtfDataFormatException e)
        {
            return FailInvalidValue<string>(field, e.Message);
        }
    }
    
    private PacketIntermediateProcessing<T> FailEof<T>(string packetName, string field)
    {
        Result = PacketResult.Err(PacketErrorCode.UnexpectedEof, packetName, field, Consumed);
        return new PacketIntermediateProcessing<T>(default!, _packetName, field, this);
    }

    private PacketIntermediateProcessing<T> FailInvalidValue<T>(string field, string? detail = null)
    {
        Result = PacketResult.Err(PacketErrorCode.InvalidValue, _packetName, field, Consumed, detail);
        return new PacketIntermediateProcessing<T>(default!, _packetName, field, this);
    }
    
    private PacketIntermediateProcessing<TNumber> ImplReadInteger<TNumber>(
        string field,
        Func<ReadOnlySpan<byte>, TNumber> readInteger
    )
    where TNumber : IBinaryInteger<TNumber>
    {
        if (ProcessingFailed) return new PacketIntermediateProcessing<TNumber>(default!, _packetName, field, this);
        var typeSize = Marshal.SizeOf<TNumber>();
        if(Remaining < typeSize) return FailEof<TNumber>(_packetName, field);
        
        var valueSequence = _sequence.Slice(Consumed, typeSize);
        
        TNumber value;
        if (valueSequence.IsSingleSegment)
        {
            value = readInteger(valueSequence.FirstSpan);
        }
        else
        {
            Span<byte> valueBytes = stackalloc byte[typeSize];
            valueSequence.CopyTo(valueBytes);
            value = readInteger(valueBytes);
        }
        
        Consumed += typeSize;
        return new PacketIntermediateProcessing<TNumber>(value, _packetName, field, this);
    }
}