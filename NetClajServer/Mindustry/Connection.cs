using System.Buffers;
using System.Buffers.Binary;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using NetClajServer.Packets;
using NetClajServer.Packets.Claj;

namespace NetClajServer.Mindustry;

public class Connection
{
    public int Id { get; init; }

    private MindustryServer _server;
    private readonly ILogger _logger;

    // Connection related properties
    private readonly TcpClient _tcp;
    private UdpClient _udp;
    public IPEndPoint? UdpEndpoint { get; set; }

    // Connection state management and shutdown tasks
    private readonly CancellationTokenSource _cts = new();
    private int _isClosed;
    private Task? _receiveLoopTask;

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

    public Task Send(IMindustryPacket packet, bool isTcp) => isTcp ? SendTcp(packet) : SendUdp(packet);

    public async Task SendTcp(IMindustryPacket packet)
    {
        var sendBytes = Serializer.Serialize(packet);
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("TCP: {ConnectionID} Sending {bytes}", Id, sendBytes[2..]);
        }

        await _tcp.GetStream().WriteAsync(sendBytes, _cts.Token);
        await _tcp.GetStream().FlushAsync();
    }

    public async Task SendUdp(IMindustryPacket packet)
    {
        var sendBytes = Serializer.Serialize(packet);
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
                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        _logger.LogDebug("{ConnectionID} Received bytes {bytes}", Id, payload.ToArray());
                    }

                    var mindustryPacket = Serializer.Deserialize(payload.ToArray());

                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        _logger.LogDebug("{ConnectionID} Got packet type {packetType}", Id, mindustryPacket.GetType().Name);
                    }

                    await _server.HandleMindustryPacket(this, mindustryPacket, true);
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

        // Buffer must contain the next packet length as a ushort.
        // One valid frame is 2 bytes of payload length + n bytes of payload
        if (buffer.Length < 2)
        {
            return false;
        }

        // Read the length of the next Mindustry packet
        Span<byte> packetLengthBytes = stackalloc byte[2];
        buffer.Slice(0, 2).CopyTo(packetLengthBytes);
        ushort packetLength = BinaryPrimitives.ReadUInt16BigEndian(packetLengthBytes);

        long frameSize = 2 + packetLength;
        if (buffer.Length < frameSize)
        {
            return false;
        }

        // Consume the frame as a payload and keep the rest in the buffer
        payload = buffer.Slice(2, packetLength);
        buffer = buffer.Slice(frameSize);

        return true;
    }

}