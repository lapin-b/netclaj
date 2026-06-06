using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Globalization;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace StressTestHarness;

public class MindustryClient
{
    private const string RoomType = "Randustry";
    public static int GeneratedPacketsPerSecond = 15;
    public static double GeneratedPacketsJitter = .3;
    private static readonly byte[] PatternFill = "Random bullshit go !"u8.ToArray();
    private static readonly int PatternFillLength = PatternFill.Length;

    private readonly TcpClient _client;
    private readonly UdpClient _udpClient;
    private readonly NetworkStream _netStream;
    
    private readonly byte[] _buffer = new byte[16 * 1024];
    private int _bufferPosition;
    private int _bufferEndPosition;
    private readonly CancellationTokenSource _ownCancel = new();
    private readonly CancellationTokenSource _linkedCancel;

    private Task _sendLoop = Task.CompletedTask;
    private Task _receiveLoop = Task.CompletedTask;
    private Task _udpReceiveLoop = Task.CompletedTask;
    private int _isClosing = 0;
    
    // Connection is host of a room
    private List<int> _connectionsInRoom = [];
    
    public int ConnectionId { get; private set; }
    public long? RoomId { get; private set; }
    public bool IsHost => RoomId != null;
    public bool IsClient => RoomId == null;

    public MindustryClient(string host, int port, CancellationToken globalCancel)
    {
        _client = new TcpClient(host, port);
        _udpClient = new UdpClient(host, port);
        _netStream = _client.GetStream();

        _linkedCancel = CancellationTokenSource.CreateLinkedTokenSource(globalCancel, _ownCancel.Token);
    }

    public async Task ExecuteHandshake()
    {
        // Get connection ID
        var connectionIdBuffer = await ReadOneFrame();
        ConnectionId = BinaryPrimitives.ReadInt32BigEndian(connectionIdBuffer.AsSpan()[2..]);
        
        // Send registration over UDP
        await _udpClient.SendAsync(new byte[] { 0xFE, 3, connectionIdBuffer[2], connectionIdBuffer[3], connectionIdBuffer[4], connectionIdBuffer[5] });
        var registredFrame = await ReadOneFrame();

        // Confirm registration was successful
        if (registredFrame[0] != 0xFE || registredFrame[1] != 3)
            throw new IOException("Unexpected packer other than registration");
    }

    public async Task CreateRoom()
    {
        await SendTcp(BuildCreateRoomPacket(RoomType), _linkedCancel.Token);
        var roomIdBuffer = await ReadOneFrame();
        if (roomIdBuffer[0] != 0xFC || roomIdBuffer[1] != 0x0B)
            throw new IOException("Expected room");

        RoomId = BinaryPrimitives.ReadInt64BigEndian(roomIdBuffer.AsSpan()[2..]);
        
        // Room configuration packet
        await SendTcp(new ReadOnlyMemory<byte>([0xFC, 12, 5, 0xFF, 0xFF, 0, 0]), _linkedCancel.Token);
    }

    public async Task JoinRoom(long roomId)
    {
        Debug.Assert(IsClient);
        var joinRoom = BuildRoomJoinPacket(roomId);
        await SendTcp(joinRoom, _linkedCancel.Token);
    }

    public void Run()
    {
        _sendLoop = SendLoop(_linkedCancel.Token).ContinueWith(LogLoopFailure, TaskContinuationOptions.OnlyOnFaulted);
        _receiveLoop = TcpReceiveLoop(_linkedCancel.Token).ContinueWith(LogLoopFailure, TaskContinuationOptions.OnlyOnFaulted);
        _udpReceiveLoop = UdpReceiveLoop(_linkedCancel.Token).ContinueWith(LogLoopFailure, TaskContinuationOptions.OnlyOnFaulted);
        return;

        void LogLoopFailure(Task task)
        {
            Console.WriteLine(task.Exception);
        }
    }
    
    public void Stop()
    {
        if (Interlocked.Exchange(ref _isClosing, 1) != 0) return;
        
        Console.WriteLine($"{ConnectionId} cancelling and closing");
        _ownCancel.Cancel();
        _client.Close();
        _udpClient.Close();

        _linkedCancel.Dispose();
        _ownCancel.Dispose();
        _client.Dispose();
        _udpClient.Dispose();
    }

    private async Task TcpReceiveLoop(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        while (!ct.IsCancellationRequested)
        {
            var frame = await ReadOneFrame();

            if (IsHost && frame[0] == 0xFC)
            {
                // Even if the client sends nonsense into the pipe, the relay will always package the random stuff
                // into a raw packet wrapper.
                
                // A connection joins the party
                if (frame[1] == 0)
                {
                    // The test harness is really simplified since it's only meant for testing
                    var connectionId = BinaryPrimitives.ReadInt32BigEndian(frame.AsSpan()[2..6]);
                    Console.WriteLine($"{connectionId} joined room {RoomId}");
                    _connectionsInRoom.Add(connectionId);
                }
                // A connection has left the party
                else if (frame[1] == 1)
                {
                    var connectionId = BinaryPrimitives.ReadInt32BigEndian(frame.AsSpan()[2..6]);
                    Console.WriteLine($"{connectionId} left room {RoomId}");
                    _connectionsInRoom.Remove(connectionId);
                }
                else if (frame[1] == 2)
                {
                    // Ignore whatever is sent here because it's just for load testing
                }
            }
            
            // As a client, we don't care much what's going in on relay side.
        }
    }

    private async Task UdpReceiveLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            // That's all the receive loop does. Empty buffers because we don't
            // care about what's actually transmitted on the line.
            await _udpClient.ReceiveAsync(ct);
        }
    }

    private async Task SendLoop(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        while (IsHost && _connectionsInRoom.Count == 0)
        {
            await Task.Delay(1000, ct);
        }

        if (GeneratedPacketsPerSecond == 0)
        {
            return;
        }

        while (!ct.IsCancellationRequested)
        {
            while (IsHost && _connectionsInRoom.Count == 0)
            {
                await Task.Delay(1000, ct);
            }
            
            var packetSize = Random.Shared.Next(0, 101) switch
            {
                < 70 => Random.Shared.Next(24, 97),
                < 95 => Random.Shared.Next(128, 513),
                _ => Random.Shared.Next(1024, 4096)
            };

            var packetContent = Random.Shared.Next(0, 3);
            var sendOverTcp = Random.Shared.NextDouble() < 0.66;

            var bytes = RandomBullshitGo(packetSize, packetContent, sendOverTcp);

            if (IsHost || sendOverTcp)
            {
                await SendTcp(bytes, ct);
            }
            else
            {
                await _udpClient.SendAsync(bytes, ct);
            }

            await Task.Delay(CalculateDelay(), ct);
        }
    }
    
    private async Task<byte[]> ReadOneFrame()
    {
        _linkedCancel.Token.ThrowIfCancellationRequested();

        while (true)
        {
            var freeSpace = _bufferEndPosition - _bufferPosition;

            if (freeSpace >= 2)
            {
                var packetLength = (_buffer[_bufferPosition] << 8) | _buffer[_bufferPosition + 1];

                if (packetLength is < 0 or > 8 * 1024)
                {
                    throw new InvalidDataException($"Invalid frame length {packetLength}");
                }

                if (freeSpace >= packetLength + 2)
                {
                    var payload = new byte[packetLength];
                    Buffer.BlockCopy(_buffer, _bufferPosition + 2, payload, 0, packetLength);
                    _bufferPosition += 2 + packetLength;
                    return payload;
                }
            }
            
            if (_bufferPosition > 0)
            {
                Buffer.BlockCopy(_buffer, _bufferPosition, _buffer, 0, _bufferEndPosition - _bufferPosition);
                _bufferEndPosition -= _bufferPosition;
                _bufferPosition = 0;
            }

            var read = await _netStream.ReadAsync(_buffer.AsMemory(_bufferEndPosition), _linkedCancel.Token);
            if (read == 0) throw new IOException("Remote closed");
            _bufferEndPosition += read;

            if (_bufferEndPosition == _buffer.Length)
            {
                throw new IOException("Buffer full, packet too long");
            }
        }
    }

    private async Task SendTcp(ReadOnlyMemory<byte> rawBytes, CancellationToken ct)
    {
        var header = ArrayPool<byte>.Shared.Rent(2);
        try
        {
            BinaryPrimitives.WriteInt16BigEndian(header.AsSpan()[..2], (short)rawBytes.Length);
            
            var segments = new ArraySegment<byte>[2];
            segments[0] = new ArraySegment<byte>(header, 0, 2);
            segments[1] = MemoryMarshal.TryGetArray(rawBytes, out var bytesSegment) ? bytesSegment : rawBytes.ToArray();
            await _client.Client.SendAsync(segments);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(header);
        }
    }
    
    private ReadOnlyMemory<byte> RandomBullshitGo(int packetSize, int packetContentClass, bool sentOverTcp)
    {
        // If is host:
        // 2 bytes for TCP packet identification
        // 4 bytes for destination identification
        // 1 byte for relaying over TCP or UDP
        var packetSizeHint = packetSize + (IsHost ? 7 : 0);
        var buffer = new ArrayBufferWriter<byte>(packetSizeHint);

        if (IsHost)
        {
            var destination = _connectionsInRoom[Random.Shared.Next(_connectionsInRoom.Count)];
            
            Span<byte> packetPreamble = [0xFC, 2, 0, 0, 0, 0, (byte)(sentOverTcp ? 1 : 0)];
            BinaryPrimitives.WriteInt32BigEndian(packetPreamble[2..6], destination);
            buffer.Write(packetPreamble);
        }
        else
        {
            var span = buffer.GetSpan(1);
            span[0] = 0x41;
            buffer.Advance(1);
            packetSize--;
        }

        switch (packetContentClass)
        {
            case 0: // Zeros
                buffer.Advance(packetSize);
                break;
            case 1: // Pattern fill
                var bytesRemaining = packetSize;
                for (; bytesRemaining >= PatternFillLength; bytesRemaining -= PatternFillLength)
                {
                    buffer.Write(PatternFill);
                }

                if (bytesRemaining > 0)
                {
                    buffer.Write(PatternFill.AsSpan()[..bytesRemaining]);
                }
                
                break;
            case 2: // Random bytes
                var randomSpan = buffer.GetSpan(packetSize);
                Random.Shared.NextBytes(randomSpan);
                buffer.Advance(packetSize);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(packetContentClass), packetContentClass, "Content class must be between 0 and 2 inclusive");
        }

        return buffer.WrittenMemory;
    }
    
    private ReadOnlyMemory<byte> BuildCreateRoomPacket(string roomType)
    {
        // Protocol packet identification,
        // Number zero as int (bypass UTF version check on the server),
        // Protocol version number (short)
        // Room type length (byte)
        Span<byte> packetStart = [0xfc, 4, 0, 0, 0, 0, 0, 4, (byte)roomType.Length];
        
        var buffer = new ArrayBufferWriter<byte>(9 + roomType.Length);
        buffer.Write(packetStart);
        buffer.Write(Encoding.ASCII.GetBytes(roomType));

        return buffer.WrittenMemory;
    }

    private ReadOnlyMemory<byte> BuildRoomJoinPacket(long roomId)
    {
        Span<byte> packetStart = [
            0xfc, 7, // Packet identifier
            0, 0, 0, 0, 0, 0, 0, 0, // Room Id
            0, // with pin
            0xff, 0xff, // blank pin
            (byte)RoomType.Length
        ];

        var buffer = new ArrayBufferWriter<byte>(packetStart.Length + RoomType.Length);
        BinaryPrimitives.WriteInt64BigEndian(packetStart[2..10], roomId);
        buffer.Write(packetStart);
        buffer.Write(Encoding.ASCII.GetBytes(RoomType));

        return buffer.WrittenMemory;
    }
    
    private int CalculateDelay()
    {
        var delay = 1000.0 / GeneratedPacketsPerSecond;
        var multiplierSign = Random.Shared.NextDouble() < 0.5 ? 1 : -1;
        var jitterPercent = Random.Shared.NextDouble() * GeneratedPacketsJitter;
        var jitterDelay = delay + jitterPercent * delay * multiplierSign;

        return (int)Math.Floor(jitterDelay);
    }
}