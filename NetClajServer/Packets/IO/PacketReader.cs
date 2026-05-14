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

    public bool TryReadByte(string packetName, string field, out byte value, out PacketResult err)
    {
        return TrySimpleReadWith(
            packetName,
            field,
            static (ref r, out v) => r.TryRead(out v),
            out value,
            out err
        );
    }
    
    public bool TryReadShortBigEndian(string packetName, string field, out short value, out PacketResult err)
    {
        return TrySimpleReadWith(
            packetName,
            field,
            static (ref r, out v) => r.TryReadBigEndian(out v),
            out value,
            out err
        );
    }
    
    public bool TryReadIntBigEndian(string packetName, string field, out int value, out PacketResult err)
    {
        return TrySimpleReadWith(
            packetName,
            field,
            static (ref r, out v) => r.TryReadBigEndian(out v),
            out value,
            out err
        );
    }
    
    public bool TryReadLongBigEndian(string packetName, string field, out long value, out PacketResult err)
    {
        return TrySimpleReadWith(
            packetName,
            field,
            static (ref r, out v) => r.TryReadBigEndian(out v),
            out value,
            out err
        );
    }

    public bool TryReadBoolean(string packetName, string field, out bool value, out PacketResult err)
    {
        if (!TryReadByte(packetName, field, out var rawValue, out err))
        {
            value = false;
            return false;
        }

        value = rawValue != 0;
        err = PacketResult.Ok();

        return true;
    }

    public bool TryReadExact(string packetName, string field, int count, out ReadOnlySequence<byte> bytes, out PacketResult err)
    {
        if (!_reader.TryReadExact(count, out bytes))
        {
            err = PacketResult.Err(
                PacketErrorCode.UnexpectedEof, packetName, field, Consumed,
                "Not enough bytes in remaining payload"
            );
            return false;
        }

        err = PacketResult.Ok();
        return true;
    }
    
    // Template method for reading integers and whatnot off the reader
    private bool TrySimpleReadWith<T>(
        string packetName,
        string field,
        ReadSimpleFunc<T> read,
        [NotNullWhen(true)]
        out T val,
        out PacketResult err)
    {
        if (read(ref _reader, out val))
        {
            err = PacketResult.Ok();
            return true;
        }

        val = default;
        err = PacketResult.Err(PacketErrorCode.UnexpectedEof, packetName, field, Consumed);
        return false;
    }
}