using System.Buffers;
using System.Buffers.Binary;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using NetClajServer.Packets;
using NetClajServer.Packets.Claj;

namespace NetClajServer.Mindustry;

public class Connection: IAsyncDisposable
{
    public int Id { get; init; }

    private readonly MindustryServer _server;
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
    
    public async Task SendTcp(IMindustryPacket packet)
    {
        var sendBytes = Serializer.Serialize(packet);
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("{ConnectionID} Sending {bytes}", Id, sendBytes);
        }
        await _tcp.GetStream().WriteAsync(sendBytes);
        await _tcp.GetStream().FlushAsync();
    }

    public async Task SendTcp(Memory<byte> buffer)
    {
        await _tcp.GetStream().WriteAsync(buffer);
        await _tcp.GetStream().FlushAsync();
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
                        _logger.LogDebug("Received bytes {bytes}", payload.ToArray());
                    }

                    var mindustryPacket = Serializer.Deserialize(payload.ToArray());

                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        _logger.LogDebug("Got packet type {packetType}", mindustryPacket.GetType().FullName);
                    }
                    
                    await _server.HandleMindustryPacket(this, mindustryPacket);
                }
                
                reader.AdvanceTo(buffer.Start, buffer.End);

                if (pipeRead.IsCompleted)
                {
                    break; // connection end
                }
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            _logger.LogWarning("{ConnectionID} Server triggered closing the connection", Id);
        }
        catch (IOException) when (token.IsCancellationRequested || !_tcp.Connected)
        {
            // Remote side broke the connection
            _logger.LogWarning("{ConnectionID} Remote closed the connection (IOException)", Id);
        }
        catch (SocketException e) when (token.IsCancellationRequested)
        {
            // Whatever happens if the socket blows up in the process of shutting down this connection
            _logger.LogWarning(e, "{ConnectionID} Something blew up in the process of shutting down", Id);
        }
        catch (EndOfStreamException) when (!_tcp.Connected)
        {
            // The remote connection closed
            _logger.LogWarning("{ConnectionID} Remote closed the connection (EndOfStreamException)", Id);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{ConnectionId} Error while processing the connection loop", Id);
            throw;
        }
        finally
        {
            // We're only escaping the loop when the connection is closed or
            // the server cancels
            await _server.HandleConnectionClosure(this);
        }
    }

    public Task Close(ConnectionCloseReason? reason = null)
    {
        return _cts.CancelAsync();
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

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _isClosed, 1) == 1)
        {
            return;
        }
        
        await _cts.CancelAsync();

        try
        {
            if (_receiveLoopTask is not null)
                await _receiveLoopTask;
        }
        catch (OperationCanceledException)
        {
            // no-op
        }
        finally
        {
            _tcp.Close();
            _tcp.Dispose();
            _cts.Dispose();
            // The UDP client isn't disposed of because it's the server's copy
        }
    }
}