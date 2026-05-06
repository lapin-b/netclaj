using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using NetClajServer.Packets;

namespace NetClajServer.Mindustry;

public partial class Connection: IAsyncDisposable
{
    public int Id { get; init; }

    // Connections
    private readonly TcpClient _tcp;
    private UdpClient _udp;
    public IPEndPoint? UdpEndpoint { get; set; }
    private readonly MindustryServer _server;
    private readonly ILogger _logger;

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
        var networkStream = _tcp.GetStream();

        try
        {
            while (!token.IsCancellationRequested)
            {
                var packetHeader = new byte[sizeof(ushort)];
                await ReadExactWithPauseAsync(networkStream, packetHeader, token);
                var nextPacketLength = BinaryPrimitives.ReadUInt16BigEndian(packetHeader);
                var packetContent = new byte[nextPacketLength];
                await ReadExactWithPauseAsync(networkStream, packetContent, token);

                var mindustryPacket = Serializer.Deserialize(new ReadOnlyMemory<byte>(packetContent));
                await _server.HandleMindustryPacket(this, mindustryPacket);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            _logger.LogWarning("{ConnectionID} Operation canceled", Id);
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
            await _server.CleanConnectionState(this);
        }
    }

    private async Task ReadExactWithPauseAsync(Stream stream, Memory<byte> buffer, CancellationToken ct)
    {
        var total = 0;
        while (total < buffer.Length)
        {
            var bytesRead = await stream.ReadAsync(buffer[total..], ct);
            if (bytesRead == 0)
            {
                throw new EndOfStreamException("Remote closed the connection");
            }

            total += bytesRead;
        }
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
            _tcp.Dispose();
            _cts.Dispose();
            // The UDP client isn't disposed of because it's the server's copy
        }
    }
}