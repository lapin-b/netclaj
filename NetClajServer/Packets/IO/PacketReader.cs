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

    public PacketReader(ref SequenceReader<byte> reader)
    {
        _reader = reader;
    }

    public PacketResult NeedByte(string packetName, string field, out byte value)
    {
        return TrySimpleReadWith(
            packetName,
            field,
            static (ref r, out v) => r.TryRead(out v),
            out value
        );
    }
    
    public PacketResult NeedShortBigEndian(string packetName, string field, out short value)
    {
        return TrySimpleReadWith(
            packetName,
            field,
            static (ref r, out v) => r.TryReadBigEndian(out v),
            out value
        );
    }
    
    public PacketResult NeedIntBigEndian(string packetName, string field, out int value)
    {
        return TrySimpleReadWith(
            packetName,
            field,
            static (ref r, out v) => r.TryReadBigEndian(out v),
            out value
        );
    }
    
    public PacketResult NeedLongBigEndian(string packetName, string field, out long value)
    {
        return TrySimpleReadWith(
            packetName,
            field,
            static (ref r, out v) => r.TryReadBigEndian(out v),
            out value
        );
    }

    public PacketResult NeedBoolean(string packetName, string field, out bool value)
    {
        value = false;
        var res = NeedByte(packetName, field, out var rawValue);
        if (res.IsFailure) return res;

        value = rawValue != 0;
        return PacketResult.Ok();
    }

    public PacketResult TryReadExact(string packetName, string field, int count, out ReadOnlySequence<byte> bytes)
    {
        if (!_reader.TryReadExact(count, out bytes))
        {
            return PacketResult.Err(
                PacketErrorCode.UnexpectedEof, packetName, field, Consumed,
                "Not enough bytes in remaining payload"
            );
        }

        return PacketResult.Ok();
    }

    public PacketResult Require(
        bool condition,
        string packetName,
        string field,
        PacketErrorCode failureCode,
        string? failureDetail
    )
    {
        return condition
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