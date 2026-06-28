using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net.Sockets;
using CommunityToolkit.HighPerformance.Buffers;
using PacketHandling;
using PacketHandling.Claj;
using PacketHandling.Framework;
using PacketHandling.IO;
using PacketHandling.Serialization;
using PacketHandling.Support;

namespace StressTestHarness;

public class MindustryClient
{
    private const string RoomType = "Randustry";
    public static int GeneratedPacketsPerSecond = 25;
    public static double GeneratedPacketsJitter = .3;
    private static readonly byte[] PatternFill = "Random bullshit go !"u8.ToArray();
    private static readonly int PatternFillLength = PatternFill.Length;

    private readonly TcpClient _client;
    private readonly UdpClient _udpClient;
    private readonly PipeReader _networkReader;
    private readonly PipeWriter _networkWriter;
    private readonly SemaphoreSlim _networkWriterLock = new(1, 1);
    
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
        var netStream = _client.GetStream();

        _networkReader = PipeReader.Create(netStream);
        _networkWriter = PipeWriter.Create(netStream);

        _linkedCancel = CancellationTokenSource.CreateLinkedTokenSource(globalCancel, _ownCancel.Token);
    }

    public async Task ExecuteHandshake()
    {
        // Get connection ID
        var connectionIdBuffer = await ReadOneFrameOf<RegisterTcpPacket>();
        ConnectionId = connectionIdBuffer.ConnectionId;
        
        // Send registration over UDP
        var registration = new RegisterUdpPacket { ConnectionId = ConnectionId };
        await SendUdp(registration);
        
        // Confirm registration was successful
        await ReadOneFrameOf<RegisterUdpPacket>();
    }

    public async Task CreateRoom()
    {
        var roomCreatePacket = new RoomCreationRequestPacket()
        {
            RoomType = new ClajRoomType { Type = RoomType },
            Version = 4
        };
        
        await SendTcp(roomCreatePacket, _linkedCancel.Token);
        var roomIdPacket = await ReadOneFrameOf<RoomLinkPacket>();
        RoomId = roomIdPacket.RoomId;
        
        // Room configuration packet
        var configPacket = new RoomConfigPacket
        {
            CanRequestHostState = true,
            IsProtectedByPin = false,
            IsPublic = true,
            MaxClients = 0,
            Pin = -1
        };

        await SendTcp(configPacket, _linkedCancel.Token);
    }

    public async Task JoinRoom(long roomId)
    {
        Debug.Assert(IsClient);
        var joinRoomPacket = new RoomJoinPacket()
        {
            RoomId = roomId,
            Pin = -1,
            RoomType = RoomType,
            WithPin = false
        };

        await SendTcp(joinRoomPacket, _linkedCancel.Token);
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
            try
            {
                var pipeRead = await _networkReader.ReadAsync(ct);
                var buffer = pipeRead.Buffer;

                while (PacketSlicer.TryReadFrame(ref buffer, out var payload))
                {
                    var packet = Serializer.Deserialize(payload);
                    packet.TransportIsTcp = true;

                    var processingTask = ProcessTcpPacket(packet);
                    if (!processingTask.IsCompletedSuccessfully)
                    {
                        await processingTask;
                    }
                }
                
                _networkReader.AdvanceTo(buffer.Start, buffer.End);
            }
            catch (SocketException e) when (
                e.SocketErrorCode is SocketError.ConnectionReset
                or SocketError.ConnectionAborted
            )
            {
                Console.WriteLine(ConnectionId + " connection was reset while reading");
                Stop();
                break;
            }
            catch (OperationCanceledException)
            {
                // no-op, we don't care
                break;
            }
            catch (Exception e)
            {
                Console.WriteLine(ConnectionId + " " + e);
                break;
            }
            
        }
    }

    private ValueTask ProcessTcpPacket(MindustryPacket packet)
    {
        // As a client, we don't care much what's going in on relay side.

        if (!IsHost)
        {
            return ValueTask.CompletedTask;
        }

        switch (packet)
        {
            case ConnectionJoinPacket connectionJoinPacket:
            {
                // The test harness is really simplified since it's only meant for testing
                var connectionId = connectionJoinPacket.ConnectionId;
                Console.WriteLine($"{connectionId} joined room {RoomId}");
                _connectionsInRoom.Add(connectionId);
                break;
            }
            case ConnectionClosedPacket connectionClosedPacket:
            {
                var connectionId = connectionClosedPacket.ConnectionId;
                Console.WriteLine($"{connectionId} left room {RoomId}");
                _connectionsInRoom.Remove(connectionId);
                break;
            }
        }

        return ValueTask.CompletedTask;
    }

    private async Task UdpReceiveLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            // That's all the receive loop does. Empty buffers because we don't
            // care about what's actually transmitted on the line.
            try
            {
                await _udpClient.ReceiveAsync(ct);
            }
            catch (OperationCanceledException)
            {
                // no-op, we don't care
            }
            catch
            {
                // no-op
                break;
            }
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

            var bytes = RandomBullshitGo(packetSize, packetContent);

            MindustryPacket packet;
            if (IsHost)
            {
                packet = new ClajPayloadWrapping
                {
                    WrappedPacketIsTcp = sendOverTcp,
                    Buffer = new ReadOnlySequence<byte>(bytes),
                    ConnectionId = _connectionsInRoom[Random.Shared.Next(_connectionsInRoom.Count)]
                };
            }
            else
            {
                packet = new GamePacket()
                {
                    Buffer = new ReadOnlySequence<byte>(bytes)
                };
            }

            if (sendOverTcp || IsHost)
            {
                await SendTcp(packet, ct);
            }
            else
            {
                await SendUdp(packet);
            }

            await Task.Delay(CalculateDelay(), ct);
        }
    }


    private async Task<T> ReadOneFrameOf<T>()
        where T: MindustryPacket
    {
        var bytes = await ReadOneFrameBytes();
        var packet = Serializer.Deserialize(bytes);
        if (packet is T expectedPacket)
        {
            return expectedPacket;
        }

        throw new Exception($"Expected packet {typeof(T)}, got {packet.GetType()}");
    }
    
    private async Task<byte[]> ReadOneFrameBytes()
    {
        _linkedCancel.Token.ThrowIfCancellationRequested();

        while (true)
        {
            var readResult = await _networkReader.ReadAsync();
            var buffer = readResult.Buffer;

            if (PacketSlicer.TryReadFrame(ref buffer, out var payload))
            {
                var payloadCopy = payload.ToArray();
                // See NetClajServer.Mindustry.Connection.ReceiveLoop for the explanation of the
                // arguments "consumed" and "examined".
                //
                // This time we're telling the pipe reader we examined to the start of unread data
                // represented by buffer.
                _networkReader.AdvanceTo(buffer.Start, buffer.Start);

                return payloadCopy;
            }
            
            // Tell the PipeReader we need more data to form a complete Mindustry frame
            _networkReader.AdvanceTo(buffer.Start, buffer.End);

            if (readResult.IsCompleted)
            {
                throw new IOException("Connection closed while waiting for a packet");
            }
        }
    }

    private async Task SendTcp(MindustryPacket packet, CancellationToken ct)
    {
        try
        {
            await _networkWriterLock.WaitAsync(ct);
            var lengthSpan = _networkWriter.GetSpan(2);
            _networkWriter.Advance(2);
            var length = Serializer.Serialize(packet, _networkWriter);
            BinaryPrimitives.WriteInt16BigEndian(lengthSpan[..2], (short)length);
            await _networkWriter.FlushAsync(ct);
        }
        finally
        {
            _networkWriterLock.Release();
        }
    }
    
    public ValueTask SendUdp(MindustryPacket packet)
    {
        using var buffer = new ArrayPoolBufferWriter<byte>();
        
        Serializer.Serialize(packet, buffer, false);
        var task = _udpClient.SendAsync(buffer.WrittenMemory);

        return task.IsCompletedSuccessfully 
            ? ValueTask.CompletedTask 
            : new ValueTask(task.AsTask());
    }
    
    private ReadOnlyMemory<byte> RandomBullshitGo(int packetSize, int packetContentClass)
    {
        var buffer = new ArrayBufferWriter<byte>(packetSize);
        var initialBytesSpan = buffer.GetSpan(2);
        initialBytesSpan[0] = 0x41;
        initialBytesSpan[1] = 0x41;
        buffer.Advance(2);
        packetSize -= 2;

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
    
    private int CalculateDelay()
    {
        var delay = 1000.0 / GeneratedPacketsPerSecond;
        var multiplierSign = Random.Shared.NextDouble() < 0.5 ? 1 : -1;
        var jitterPercent = Random.Shared.NextDouble() * GeneratedPacketsJitter;
        var jitterDelay = delay + jitterPercent * delay * multiplierSign;

        return (int)Math.Floor(jitterDelay);
    }
}