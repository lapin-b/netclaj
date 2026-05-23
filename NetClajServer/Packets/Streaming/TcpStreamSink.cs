using System.Buffers;
using NetClajServer.Mindustry;

namespace NetClajServer.Packets.Streaming;

public class TcpStreamSink: IDisposable
{
    //private const int ChunkSize = 2048;
    // TODO: Uncomment this ^
    private const int ChunkSize = 64;
    
    private readonly Connection _connection;
    private readonly int _streamId;
    private readonly byte[] _buffer;
    private int _position = 0;

    public TcpStreamSink(Connection connection, int streamId)
    {
        _connection = connection;
        _streamId = streamId;
        _buffer = ArrayPool<byte>.Shared.Rent(ChunkSize);
    }

    public async ValueTask Write(ReadOnlyMemory<byte> toWrite)
    {
        while (!toWrite.IsEmpty)
        {
            var freeSpaceRemainingInBuffer = _buffer.Length - _position;
            
            if (freeSpaceRemainingInBuffer == 0)
            {
                // Yeet a stream packet, the flush resets the position to zero
                await CutChunk(false);
                freeSpaceRemainingInBuffer = _buffer.Length;
            }

            // Get the size of the copy step. It's either the remaining bytes of
            // the stream to write or whatever free space there is in the buffer.
            var copySize = Math.Min(freeSpaceRemainingInBuffer, toWrite.Length);
            
            // Copy up to whatever size has been chosen by slicing the buffer provided
            // by the caller up to the copy size
            toWrite[..copySize].CopyTo(_buffer.AsMemory(_position));
            _position += copySize;
            
            // Slice the copied slice off the remaining buffer to write and start again.
            // An oversized write is handled here.
            toWrite = toWrite[copySize..];
        }
    }

    public Task Complete(bool isLast)
    {
        return _position > 0 || isLast ? CutChunk(isLast) : Task.CompletedTask;
    }

    private async Task CutChunk(bool last)
    {
        await _connection.SendTcp(new StreamChunk()
        {
            StreamId = _streamId,
            Chunk = _buffer.AsMemory(0, _position),
            IsLastChunk = last
        });
        
        _position = 0;
    }
    
    public void Dispose()
    {
        ArrayPool<byte>.Shared.Return(_buffer);
    }
}