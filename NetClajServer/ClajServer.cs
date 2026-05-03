using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;

namespace NetClajServer;

public class ClajServer
{
    private readonly ClajServerConfiguration _config;
    private readonly ILogger<ClajServer> _logger;
    private readonly TcpListener _tcpListener;
    private Task? _tcpServerTask;
    private Task? _udpServerTask;
    private readonly CancellationTokenSource _stopAcceptingConnections;
    private readonly UdpClient _udpListener;
    private CancellationToken StopAcceptingConnectionsToken => _stopAcceptingConnections.Token;

    public ClajServer(ClajServerConfiguration config, ILogger<ClajServer> logger)
    {
        _config = config;
        _logger = logger;

        _tcpListener = new TcpListener(IPAddress.Parse(config.IPAddress), config.Port);
        _udpListener = new UdpClient(new IPEndPoint(IPAddress.Parse(config.IPAddress), config.Port));
        _stopAcceptingConnections = new CancellationTokenSource();
    }

    public void Start()
    {
        _logger.LogDebug("Starting TCP listener");
        _tcpListener.Start();
        _tcpServerTask = Task.Run(ServerAcceptLoop, StopAcceptingConnectionsToken);
        _udpServerTask = Task.Run(UdpServerLoop, StopAcceptingConnectionsToken);
    }

    public void Close()
    {
        _logger.LogDebug("Canceling TCP listener task token");
        _stopAcceptingConnections.Cancel();
    }

    private async Task ServerAcceptLoop()
    {
        while (!_stopAcceptingConnections.IsCancellationRequested)
        {
            _logger.LogDebug("Waiting for a new TCP connection");
            var client = await _tcpListener.AcceptTcpClientAsync(StopAcceptingConnectionsToken);
            client.Close();
        }

        _tcpListener.Stop();
    }

    private async Task UdpServerLoop()
    {
        while (!_stopAcceptingConnections.IsCancellationRequested)
        {
            _logger.LogDebug("Waiting for an UDP message");
            var message = await _udpListener.ReceiveAsync(StopAcceptingConnectionsToken);
        }
        
        _udpListener.Close();
    }
}