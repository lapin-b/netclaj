using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NetClajServer;

public class ClajServer
{
    private readonly ClajServerConfiguration _config;
    private readonly TcpListener _tcpListener;
    private Task? _tcpServerTask;
    private readonly CancellationTokenSource _stopAcceptingConnections;
    private CancellationToken StopAcceptingConnectionsToken => _stopAcceptingConnections.Token;

    public ClajServer(ClajServerConfiguration config)
    {
        _config = config;
        _tcpListener = new TcpListener(IPAddress.Parse(config.IPAddress), config.Port);
        _stopAcceptingConnections = new CancellationTokenSource();
    }

    public void Start()
    {
        Console.WriteLine("Starting");
        _tcpListener.Start();
        _tcpServerTask = Task.Run(ServerAcceptLoop, StopAcceptingConnectionsToken);
    }

    public void Close()
    {
        Console.WriteLine("Canceling");
        _stopAcceptingConnections.Cancel();
    }

    private async Task ServerAcceptLoop()
    {
        while (!_stopAcceptingConnections.IsCancellationRequested)
        {
            var client = await _tcpListener.AcceptTcpClientAsync(StopAcceptingConnectionsToken);
            await client.GetStream().WriteAsync("Hello world !\n"u8.ToArray(), StopAcceptingConnectionsToken);
            client.Close();
        }

        _tcpListener.Stop();
    }
}