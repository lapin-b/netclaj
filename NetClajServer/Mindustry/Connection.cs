using System.Buffers;
using System.Buffers.Binary;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
using CommunityToolkit.HighPerformance.Buffers;
using Microsoft.Extensions.Logging;
using NetClajServer.Metrics;
using NetClajServer.Packets;
using NetClajServer.Packets.Framework;
using NetClajServer.Packets.Streaming;

namespace NetClajServer.Mindustry;

public partial class Connection
{
    public int Id { get; }
    public long? ParticipatesInRoomId { get; set; }

    private readonly MindustryServer _server;
    private readonly SessionsManager _sessionsManager;
    private readonly ILogger<Connection> _logger;
    private readonly ServerMetrics _metrics;

    // Connection related properties
    private readonly TcpClient _tcp;
    private readonly UdpClient _udp;
    public IPEndPoint? UdpEndpoint { get; set; }
    public bool IsConnected => _tcp.Connected && UdpEndpoint != null;
    private PipeReader _networkReader;
    private PipeWriter _networkWriter;
    private readonly SemaphoreSlim _networkWriterLock = new(1, 1);

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
        SessionsManager sessionsManager,
        ILogger<Connection> logger,
        ServerMetrics metrics)
    {
        Id = connectionId;
        _tcp = tcp;
        _udp = udp;
        _server = server;
        _sessionsManager = sessionsManager;
        _logger = logger;
        _metrics = metrics;
        
        var networkStream = _tcp.GetStream();
        _networkReader = PipeReader.Create(networkStream);
        _networkWriter = PipeWriter.Create(networkStream);
    }

    public void Start(CancellationToken serverToken)
    {
        // The server or us can request the TCP read loop to be broken
        var linked = CancellationTokenSource.CreateLinkedTokenSource(serverToken, _cts.Token);
        _receiveLoopTask = ReceiveLoop(linked.Token);
    }
    
    public ValueTask Send(MindustryPacket packet, bool isTcp) => isTcp ? SendTcp(packet) : SendUdp(packet);

    public async ValueTask SendTcp(MindustryPacket packet)
    {
        if (Volatile.Read(ref _closeHasStarted) == 1) return;

        try
        {
            await _networkWriterLock.WaitAsync(_cts.Token);
            var lengthSpan = _networkWriter.GetSpan(2);
            _networkWriter.Advance(2);
            var length = Serializer.Serialize(packet, _networkWriter);
            BinaryPrimitives.WriteInt16BigEndian(lengthSpan[..2], (short)length);
            await _networkWriter.FlushAsync(_cts.Token);
        }
        finally
        {
            _networkWriterLock.Release();
        }
    }
    
    public ValueTask SendUdp(MindustryPacket packet)
    {
        if (Volatile.Read(ref _closeHasStarted) == 1) return ValueTask.CompletedTask;

        using var buffer = new ArrayPoolBufferWriter<byte>();
        
        Serializer.Serialize(packet, buffer, false);
        var task = _udp.SendAsync(buffer.WrittenMemory, UdpEndpoint, _cts.Token);

        return task.IsCompletedSuccessfully 
            ? ValueTask.CompletedTask 
            : new ValueTask(task.AsTask());
    }
    
    public ValueTask SendUdp(ReadOnlyMemory<byte> packet)
    {
        if (Volatile.Read(ref _closeHasStarted) == 1) return ValueTask.CompletedTask;
        LogSentBytes("UDP", Id, packet);
        var task = _udp.SendAsync(packet, UdpEndpoint, _cts.Token);

        return task.IsCompletedSuccessfully 
            ? ValueTask.CompletedTask 
            : new ValueTask(task.AsTask());
    }
    
    public async Task SendStreaming(IStreamablePacket packet)
    {
        var streamHead = new StreamHead
        {
            TotalBytes = packet.StreamTotalPacketSize(),
            InnerPacketIdentifier = packet.GetPacketIdentifier(),
            IsCompressed = false
        };

        await SendTcp(streamHead);

        using var tcpSink = new TcpStreamSink(this, streamHead.Id);
        await packet.StreamChunks(tcpSink);
        await tcpSink.Complete(true);
    }
    
    public ValueTask ProcessDeserializedPacket(MindustryPacket mindustryPacket)
    {
        _metrics.IncrementIncomingPacketsProcessed();

        /*
         * TODO: Fast path processing for game packets when the connection is part of a room.
         * Bypass fetching the game packet handler from the handlers registry.
         */
        
        // A player joins the room as a client and will send a few packets that might arrive before the
        // room host is aware of this player. To not lose anything, buffer the game packets until the host
        // is aware of the player.
        if (mindustryPacket is GamePacket raw && ParticipatesInRoomId == null)
        {
            LogNotYetParticipatingInRoom(Id);
            RawPacketsQueue.Writer.TryWrite(new MaterializedGamePacket(raw));
            return ValueTask.CompletedTask;
            // ^ The channel handles excess game packets being written and drops them if needed
        }

        return _server.HandleMindustryPacket(this, mindustryPacket);
    }
    
    private async Task ReceiveLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                var pipeRead = await _networkReader.ReadAsync(token);
                var buffer = pipeRead.Buffer;

                while (TryReadFrame(ref buffer, out var payload))
                {
                    DebugRecvBytes(payload);
                    var mindustryPacket = Serializer.Deserialize(payload);
                    mindustryPacket.TransportIsTcp = true;
                    DebugRecvPacket(mindustryPacket);

                    var task = ProcessDeserializedPacket(mindustryPacket);
                    if (task.IsCompletedSuccessfully) continue;
                    await task;
                }

                _networkReader.AdvanceTo(buffer.Start, buffer.End);

                if (pipeRead.IsCompleted)
                {
                    RequestClose(ArcNetDcReason.Closed);
                    break; // connection end
                }
            }
            catch (OperationCanceledException e) when (!_cts.IsCancellationRequested)
            {
                _logger.LogWarning(e, "OperationCanceledException caught. This must have bubbled up the stack");
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("{@Connection} canceled. Closing", this);
                RequestClose(ArcNetDcReason.Closed);
                break;
            }
            catch (SocketException) when (token.IsCancellationRequested)
            {
                RequestClose(ArcNetDcReason.Closed);
                break;
            }
            // Exception while sending things in the handlers
            catch (SocketException e) when (e.SocketErrorCode is SocketError.ConnectionReset or SocketError.ConnectionAborted)
            {
                HandleConnectionReset();
                break;
            }
            // Exception when reading from the pipe is not possible
            catch (IOException e) when (e.GetBaseException() is SocketException { SocketErrorCode: SocketError.ConnectionReset or SocketError.ConnectionAborted })
            {
                HandleConnectionReset();
                break;
            }
            catch (IOException e)
            {
                _logger.LogWarning(e, "IOException while processing a TCP packet for {@Connection}", this);
                RequestClose(ArcNetDcReason.Error);
                break;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "{@Connection} something blew up", this);
                RequestClose(ArcNetDcReason.Error);
                break;
            }
        }

        return;
        
        void HandleConnectionReset()
        {
            _logger.LogInformation("Connection {@Connection} has been reset", this);
            RequestClose(ArcNetDcReason.Closed);
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
            await _sessionsManager.CleanupConnectionState(this, _closureReason);
            try { await _receiveLoopTask; }
            catch { /* no-op */ }
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