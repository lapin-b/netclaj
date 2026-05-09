using System.Buffers;
using System.Buffers.Binary;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using NetClajServer.Packets;
using NetClajServer.Packets.Framework;

namespace NetClajServer.Mindustry;

public partial class Connection
{
    private const int BufferSize = 32;
    public int Id { get; set; }

    private MindustryServer _server;
    private readonly ILogger _logger;

    // Connection related properties
    private readonly TcpClient _tcp;
    private UdpClient _udp;
    public IPEndPoint? UdpEndpoint { get; set; }
    public long? ParticipatesInRoomId { get; set; } = null;
    public bool IsConnected => _tcp.Connected;

    // Connection state management and shutdown tasks
    private readonly CancellationTokenSource _cts = new();
    private int _isClosed;
    private Task? _receiveLoopTask;
    public Queue<GamePacket> RawPacketsQueue { get; } = new(16);

    public Connection(TcpClient tcp,
        UdpClient udp,
        MindustryServer server,
        ILogger logger)
    {
        _tcp = tcp;
        _udp = udp;
        _server = server;
        _logger = logger;
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
        var sendBytes = Serializer.Serialize(packet, true);
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("TCP: {ConnectionID} Sending {bytes}", Id, sendBytes);
        }

        await _tcp.GetStream().WriteAsync(sendBytes, _cts.Token);
        await _tcp.GetStream().FlushAsync();
    }

    public async Task SendUdp(MindustryPacket packet)
    {
        var sendBytes = Serializer.Serialize(packet, false);
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("UDP: {ConnectionID} Sending {bytes}", Id, sendBytes);
        }

        await _udp.SendAsync(sendBytes, UdpEndpoint, _cts.Token);
    }

    private async Task ReceiveLoop(CancellationToken token)
    {
        var reader = PipeReader.Create(_tcp.GetStream());

        try
        {
            while (!token.IsCancellationRequested)
            {
                var pipeRead = await reader.ReadAsync(token);
                var buffer = pipeRead.Buffer;

                while (TryReadFrame(ref buffer, out var payload))
                {
                    LogBytesRecv(Id, payload.ToArray());
                    var mindustryPacket = Serializer.Deserialize(payload.ToArray());
                    mindustryPacket.IsTcp = true;

                    if (mindustryPacket is not KeepAlivePacket)
                    {
                        LogPacketTypeRecv(Id, mindustryPacket.GetType().Name);
                    }

                    // A player joins the room as a client and will send a few packets that might arrive before the
                    // room host is aware of this player. To not lose anything, buffer the game packets until the host
                    // is aware of the player.
                    if (mindustryPacket is GamePacket raw && ParticipatesInRoomId == null && RawPacketsQueue.Count < BufferSize)
                    {
                        LogNotYetParticipatingInRoom(Id);
                        RawPacketsQueue.Enqueue(raw);
                    }
                    // The buffer is here to prevent filling up the server's memory completely
                    else if (RawPacketsQueue.Count >= BufferSize)
                    {
                        _logger.LogWarning("{ConnectionId} Raw packets queue length exceeded {bufferSize} packets. Dropping.", BufferSize, Id);
                    }
                    else
                    {
                        await _server.HandleMindustryPacket(this, mindustryPacket);
                    }
                }

                reader.AdvanceTo(buffer.Start, buffer.End);

                if (pipeRead.IsCompleted)
                {
                    break; // connection end
                }
            }
        }
        catch (OperationCanceledException)
        {
            // no-op
        }
        catch (IOException)
        {
            // Remote side broke the connection
            _logger.LogWarning("{ConnectionID} Remote closed the connection (IOException)", Id);
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
            // We're only escaping the loop when the connection is closed or
            // the server cancels
            InternalClosing(ConnectionCloseReason.Error);
        }
    }

    public void Close(ConnectionCloseReason? reason = null)
    {
        _cts.Cancel();
        InternalClosing(reason);
    }

    private void InternalClosing(ConnectionCloseReason? reason)
    {
        if (Interlocked.Exchange(ref _isClosed, 1) == 1)
        {
            return;
        }

        _logger.LogInformation("{ConnectionID} Closing connection", Id);
        
        _cts.Cancel();
        _server.NotifyConnectionClosure(this, reason);
        UdpEndpoint = null;
        ParticipatesInRoomId = null;
        RawPacketsQueue.Clear();
        
        _tcp.Close();
        _tcp.Dispose();
        _cts.Dispose();
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
        Span<byte> packetLengthBytes = stackalloc byte[2];
        buffer.Slice(0, 2).CopyTo(packetLengthBytes);
        var packetLength = BinaryPrimitives.ReadUInt16BigEndian(packetLengthBytes);

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