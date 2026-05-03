using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace NetClajServer.Mindustry;

public class MindustryServer
{
    private readonly ILogger<MindustryServer> _logger;

    private readonly TcpListener _tcpListener;
    private readonly UdpClient _udpListener;

    // The cancellation token source is set when the server starts and
    // unset when the server stops. The downstream code shouldn't get
    // a null value.
    private CancellationTokenSource _stopAcceptingConnections = null!;
    private CancellationToken ServerStopToken => _stopAcceptingConnections.Token;

    public Dictionary<long, Connection> Connections = new();
 
    public MindustryServer(ClajServerConfiguration config, ILogger<MindustryServer> logger)
    {
        _logger = logger;
        
        _tcpListener = new TcpListener(IPAddress.Parse(config.IPAddress), config.Port);
        _udpListener = new UdpClient(new IPEndPoint(IPAddress.Parse(config.IPAddress), config.Port));
    }

    public void Start()
    {
        _logger.LogDebug("Starting TCP listener");
        _stopAcceptingConnections = new CancellationTokenSource();
        _tcpListener.Start();
        Task.Run(ServerAcceptLoop, ServerStopToken);
        Task.Run(UdpServerLoop, ServerStopToken);
    }

    public void Close()
    {
        _logger.LogDebug("Canceling listeners");
        _stopAcceptingConnections.Cancel();
        _stopAcceptingConnections.Dispose();
    }

    private async Task ServerAcceptLoop()
    {
        while (!_stopAcceptingConnections.IsCancellationRequested)
        {
            _logger.LogDebug("Waiting for a new TCP connection");
            var client = await _tcpListener.AcceptTcpClientAsync(ServerStopToken);
            
            var connection = new Connection(client, _udpListener, OnConnectionClosed)
            {
                Id = GenerateConnectionId()
            };
            
            Connections.Add(connection.Id, connection);

            _ = Task.Run(() => connection.ReceiveLoop(ServerStopToken), ServerStopToken);
        }

        _tcpListener.Stop();
    }

    private async Task UdpServerLoop()
    {
        while (!_stopAcceptingConnections.IsCancellationRequested)
        {
            _logger.LogDebug("Waiting for an UDP message");
            var message = await _udpListener.ReceiveAsync(ServerStopToken);
        }
        
        _udpListener.Close();
    }

    private int GenerateConnectionId()
    {
        int connectionId;
        do
        {
            connectionId = Random.Shared.Next(int.MinValue, int.MaxValue);
        } while (Connections.ContainsKey(connectionId));

        return connectionId;
    }

    private void OnConnectionClosed(Connection connection)
    {
        _logger.LogInformation("Connection {ConnectionId} closed. Freeing resources", connection.Id);
        Connections.Remove(connection.Id);
        connection.Dispose();
    }
}