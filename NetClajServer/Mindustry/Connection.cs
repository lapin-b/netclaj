using System.Buffers;
using System.Buffers.Binary;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.IO;
using NetClajServer.Metrics;
using NetClajServer.Packets;
using NetClajServer.Packets.Framework;
using NetClajServer.Packets.Streaming;

namespace NetClajServer.Mindustry;

public partial class Connection
{
    private static RecyclableMemoryStreamManager _memoryStreamManager = new();
    
    public int Id { get; }
    public long? ParticipatesInRoomId { get; set; }

    private readonly MindustryServer _server;
    private readonly ILogger<Connection> _logger;
    private readonly ServerMetrics _metrics;

    // Connection related properties
    private readonly TcpClient _tcp;
    private readonly UdpClient _udp;
    private readonly NetworkStream _tcpStream;
    public IPEndPoint? UdpEndpoint { get; set; }
    public bool IsConnected => _tcp.Connected && UdpEndpoint != null;

    // Connection state management and shutdown tasks
    private readonly CancellationTokenSource _cts = new();
    private Task _receiveLoopTask = Task.CompletedTask;
    
    // Making connection closure more robust
    private int _closeHasStarted = 0;
    private ArcNetDcReason? _closureReason;
    private readonly TaskCompletionSource _closedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public Task Closed => _closedTcs.Task;
    
    public Channel<MaterializedGamePacket> RawPacketsQueue { get; } = Channel.CreateBounded<MaterializedGamePacket>(
        new BoundedChannelOptions(Constants.ConnectionRawPacketsBuffer)
        {
            FullMode = BoundedChannelFullMode.DropWrite
        }
    );

    public Connection(
        int connectionId,
        TcpClient tcp,
        UdpClient udp,
        MindustryServer server,
        ILogger<Connection> logger,
        ServerMetrics metrics)
    {
        Id = connectionId;
        _tcp = tcp;
        _udp = udp;
        _server = server;
        _logger = logger;
        _metrics = metrics;
        _tcpStream = _tcp.GetStream();
    }

    public void Start(CancellationToken serverToken)
    {
        // The server or us can request the TCP read loop to be broken
        var linked = CancellationTokenSource.CreateLinkedTokenSource(serverToken, _cts.Token);
        _receiveLoopTask = ReceiveLoop(linked.Token);
    }
    
    public Task Send(MindustryPacket packet, bool isTcp) => isTcp ? SendTcp(packet) : SendUdp(packet);

    public async Task SendTcp(MindustryPacket packet)
    {
        if (Volatile.Read(ref _closeHasStarted) == 1) return;

        await using var memoryStream = _memoryStreamManager.GetStream();
        await using var binaryWriter = new BinaryWriter(memoryStream);
        
        var sendBytes = Serializer.Serialize(packet, memoryStream, binaryWriter);
        LogSentBytes("TCP", Id, sendBytes);
        await _tcpStream.WriteAsync(sendBytes, _cts.Token);
    }

    public async Task SendTcp(List<MindustryPacket> packets)
    {
        if (Volatile.Read(ref _closeHasStarted) == 1) return;

        await using var memoryStream = _memoryStreamManager.GetStream();
        await using var binaryWriter = new BinaryWriter(memoryStream);
        
        var sendBytes = Serializer.Serialize(packets, memoryStream, binaryWriter);
        LogSentBytes("TCP bulk", Id, sendBytes);
        await _tcpStream.WriteAsync(sendBytes, _cts.Token);
    }

    public async Task SendUdp(MindustryPacket packet)
    {
        if (Volatile.Read(ref _closeHasStarted) == 1) return;
        
        await using var memoryStream = _memoryStreamManager.GetStream();
        await using var binaryWriter = new BinaryWriter(memoryStream);
        
        var sendBytes = Serializer.Serialize(packet, memoryStream, binaryWriter, false);
        LogSentBytes("UDP", Id, sendBytes);
        await _udp.SendAsync(sendBytes, UdpEndpoint, _cts.Token);
    }
    
    public async Task SendUdp(ReadOnlyMemory<byte> packet)
    {
        if (Volatile.Read(ref _closeHasStarted) == 1) return;
        LogSentBytes("UDP", Id, packet);
        await _udp.SendAsync(packet, UdpEndpoint, _cts.Token);
    }
    
    public async Task SendStreaming(IStreamablePacket packet)
    {
        var streamHead = new StreamHead
        {
            TotalBytes = packet.StreamTotalPacketSize(),
            InnerPacketIdentifier = packet.GetPacketIdentifier(),
            IsCompressed = false
            // TODO: Implement compression by measuring if the initial payload is compressible or not.
        };

        await SendTcp(streamHead);

        using var tcpSink = new TcpStreamSink(this, streamHead.Id);
        await packet.StreamChunks(tcpSink);
        await tcpSink.Complete(true);
    }
    
    public Task ProcessDeserializedPacket(MindustryPacket mindustryPacket)
    {
        _metrics.IncrementIncomingPacketsProcessed();

        // A player joins the room as a client and will send a few packets that might arrive before the
        // room host is aware of this player. To not lose anything, buffer the game packets until the host
        // is aware of the player.
        if (mindustryPacket is GamePacket raw && ParticipatesInRoomId == null)
        {
            LogNotYetParticipatingInRoom(Id);
            RawPacketsQueue.Writer.TryWrite(new MaterializedGamePacket(raw));
            return Task.CompletedTask;
            // ^ The channel handles excess game packets being written and drops them if needed
        }

        return _server.HandleMindustryPacket(this, mindustryPacket);
    }
    
    private async Task ReceiveLoop(CancellationToken token)
    {
        var reader = PipeReader.Create(_tcpStream);

        try
        {
            while (!token.IsCancellationRequested)
            {
                var pipeRead = await reader.ReadAsync(token);
                var buffer = pipeRead.Buffer;

                while (TryReadFrame(ref buffer, out var payload))
                {
                    DebugRecvBytes(payload);
                    var mindustryPacket = Serializer.Deserialize(payload);
                    mindustryPacket.IsTcp = true;
                    DebugRecvPacket(mindustryPacket);
                    await ProcessDeserializedPacket(mindustryPacket);
                }

                reader.AdvanceTo(buffer.Start, buffer.End);

                if (pipeRead.IsCompleted)
                {
                    break; // connection end
                }
            }
        }
        catch (OperationCanceledException e)
        {
            if (!_cts.IsCancellationRequested)
            {
                _logger.LogWarning(e, "Task cancellation caught. This must have bubbled up the stack");
            }
        }
        catch (IOException e)
        {
            // Remote side broke the connection or something
            _logger.LogWarning(e, "{ConnectionID} Remote closed the connection (IOException)", Id);
        }
        catch (SocketException e) when (token.IsCancellationRequested)
        {
            // Whatever happens if the socket blows up in the process of shutting down this connection
            _logger.LogWarning(e, "{ConnectionID} Something blew up in the process of shutting down", Id);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{ConnectionId} something blew up", Id);
        }
        finally
        {
            RequestClose(ArcNetDcReason.Error);
        }
    }
    
    public void RequestClose(ArcNetDcReason? reason)
    {
        if (Interlocked.Exchange(ref _closeHasStarted, 1) != 0) return;

        _closureReason ??= reason;
        _cts.Cancel();
        _tcp.Close();

        _ = Task.Run(FinalizeConnectionClosure);
    }

    private async Task FinalizeConnectionClosure()
    {
        try
        {
            try { await _receiveLoopTask; }
            catch { /* no-op */ }

            await _server.NotifyConnectionClosure(this, _closureReason);
        }
        finally
        {
            UdpEndpoint = null;
            ParticipatesInRoomId = null;
            _tcp.Dispose();
            _cts.Dispose();
            _closedTcs.TrySetResult();
        }
    }

    
    private bool TryReadFrame(
        ref ReadOnlySequence<byte> buffer,
        out ReadOnlySequence<byte> payload
    )
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

        // Extract a frame while cutting-off the length. This allows the packet processing to be agnostic
        // of the transport of the original packet.
        payload = buffer.Slice(2, packetLength);
        buffer = buffer.Slice(frameSize);

        return true;
    }
}