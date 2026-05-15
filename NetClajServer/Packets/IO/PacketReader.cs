using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace NetClajServer.Packets.IO;

/// <summary>
/// Wrapper around a <see cref="SequenceReader{T}"/> to make packet parsing less
/// painful to write.
/// </summary>
public ref struct PacketReader
{
    private SequenceReader<byte> _reader;
    private delegate bool ReadSimpleFunc<T>(ref SequenceReader<byte> reader, [NotNullWhen(true)] out T value);

    public long Consumed => _reader.Consumed;
    public long Remaining => _reader.Remaining;
    
    // Keep the error state internally and skip deserializing the packet if the error is set
    public PacketResult Result = PacketResult.Ok();
    public bool ProcessingFailed => Result.IsFailure;

    public PacketReader(ref SequenceReader<byte> reader)
    {
        _reader = reader;
    }

    public void NeedByte(string packetName, string field, out byte value)
    {
        value = 0;
        if (Result.IsFailure) return;
        
        Result = TrySimpleReadWith(
            packetName,
            field,
            static (ref r, out v) => r.TryRead(out v),
            out value
        );
    }
    
    public void NeedShortBigEndian(string packetName, string field, out short value)
    {
        value = 0;
        if (Result.IsFailure) return;
        
        Result = TrySimpleReadWith(
            packetName,
            field,
            static (ref r, out v) => r.TryReadBigEndian(out v),
            out value
        );
    }
    
    public void NeedIntBigEndian(string packetName, string field, out int value)
    {
        value = 0;
        if (Result.IsFailure) return;
        
        Result = TrySimpleReadWith(
            packetName,
            field,
            static (ref r, out v) => r.TryReadBigEndian(out v),
            out value
        );
    }
    
    public void NeedLongBigEndian(string packetName, string field, out long value)
    {
        value = 0;
        if (Result.IsFailure) return;
        
        Result = TrySimpleReadWith(
            packetName,
            field,
            static (ref r, out v) => r.TryReadBigEndian(out v),
            out value
        );
    }

    public void NeedBoolean(string packetName, string field, out bool value)
    {
        value = false;
        NeedByte(packetName, field, out var rawValue);
        if (Result.IsFailure) return;

        value = rawValue != 0;
    }

    public void NeedReadExact(string packetName, string field, int count, out ReadOnlySequence<byte> bytes)
    {
        bytes = default;
        if (Result.IsFailure) return;
        
        if (!_reader.TryReadExact(count, out bytes))
        {
            Result = PacketResult.Err(
                PacketErrorCode.UnexpectedEof, packetName, field, Consumed,
                $"Not enough bytes in remaining payload ({count} requested, {Remaining} remaining)"
            );
        }
    }

    public string DebugRemaining()
    {
        return Convert.ToHexString(_reader.UnreadSequence.ToArray());
    }

    public void Require(
        bool condition,
        string packetName,
        string field,
        PacketErrorCode failureCode,
        string? failureDetail
    )
    {
        if (Result.IsFailure) return;
        
        Result = condition
            ? PacketResult.Ok()
            : PacketResult.Err(failureCode, packetName, field, Consumed, failureDetail);
    }
    
    // Template method for reading integers and whatnot off the reader
    private PacketResult TrySimpleReadWith<T>(
        string packetName,
        string field,
        ReadSimpleFunc<T> read,
        [NotNullWhen(true)]
        out T val)
    {
        if (read(ref _reader, out val))
        {
            return PacketResult.Ok();
        }

        val = default;
        return PacketResult.Err(PacketErrorCode.UnexpectedEof, packetName, field, Consumed);
    }
}