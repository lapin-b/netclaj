using System.Net;
using System.Net.Sockets;

namespace NetClajServer.Mindustry;

public class Connection: IAsyncDisposable
{
    public int Id { get; init; }

    private readonly TcpClient _tcp;
    public UdpClient _udp;
    private readonly Action<Connection> _onClosed;

    private readonly CancellationTokenSource _cts = new();
    private Task? _receiveLoopTask;
    private int _isClosed;
    
    public Connection(TcpClient tcp, UdpClient udp, Action<Connection> onClosed)
    {
        _tcp = tcp;
        _udp = udp;
        _onClosed = onClosed;
    }

    public void Start(CancellationToken serverToken)
    {
        // The server or us can request the TCP read loop to be broken
        var linked = CancellationTokenSource.CreateLinkedTokenSource(serverToken, _cts.Token);
        _receiveLoopTask = ReceiveLoop(linked.Token);
    }

    private async Task ReceiveLoop(CancellationToken token)
    {
        var networkStream = _tcp.GetStream();
        var binaryReader = new BinaryReader(_tcp.GetStream());
        
        var buffer = new byte[_tcp.ReceiveBufferSize];
        try
        {
            while (!token.IsCancellationRequested)
            {
                var readBytesCount = await networkStream.ReadAsync(buffer, token);
                if (readBytesCount == 0) break; // remote closed the connection
                await networkStream.WriteAsync(buffer.AsMemory(0, readBytesCount), token);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // Connection closure was requested
        }
        catch (IOException) when (token.IsCancellationRequested || !_tcp.Connected)
        {
            // Remote side broke the connection
        }
        catch (SocketException) when (token.IsCancellationRequested)
        {
            // Whatever happens if the socket blows up in the process of shutting down this connection
        }
        finally
        {
            // We're only escaping the loop when the connection is closed or
            // the server cancels
            if (Interlocked.Exchange(ref _isClosed, 1) == 0)
            {
                _onClosed(this);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _isClosed, 1) != 0) return;
        
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