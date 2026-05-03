using System.Net;
using System.Net.Sockets;

namespace NetClajServer.Mindustry;

public class Connection: IDisposable
{
    public int Id { get; init; }

    private TcpClient _tcp;
    public UdpClient _udp;
    private IPEndPoint? UdpEndpoint { get; set; }
    private readonly Action<Connection> _onConnectionClosed;
    
    public Connection(TcpClient tcp, UdpClient udp, Action<Connection> onConnectionClosed)
    {
        _tcp = tcp;
        _udp = udp;
        _onConnectionClosed = onConnectionClosed;
    }

    public async Task ReceiveLoop(CancellationToken token)
    {
        var buffer = new byte[_tcp.ReceiveBufferSize];
        while (!token.IsCancellationRequested && _tcp.Connected)
        {
            var count = await _tcp.GetStream().ReadAsync(buffer, token);
            var message = new Memory<byte>(buffer, 0, count);
            await _tcp.GetStream().WriteAsync(message, token);
        }

        // If the server cancels this task (i.e., it's shutting down), or the client disconnects
        // let the server know it needs to cleanup this connection
        _onConnectionClosed(this);
    }

    public void Dispose()
    {
        _tcp.Dispose();
        // The UDP client isn't disposed of because it's the server's
    }
}