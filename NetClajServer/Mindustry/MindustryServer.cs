using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using NetClajServer.Datastructures;

namespace NetClajServer.Mindustry;

public class MindustryServer
{
    private readonly ILogger<MindustryServer> _logger;

    private readonly TcpListener _tcpListener;
    private readonly UdpClient _udpListener;
    private Task? _tcpServerTask;
    private Task? _udpServerTask;

    // The cancellation token source is set when the server starts and
    // unset when the server stops. The downstream code shouldn't get
    // a null value.
    private CancellationTokenSource? _cts;

    private ConcurrentDictionary<long, Connection> Connections = new();
 
    public MindustryServer(ClajServerConfiguration config, ILogger<MindustryServer> logger)
    {
        _logger = logger;
        
        _tcpListener = new TcpListener(IPAddress.Parse(config.IPAddress), config.Port);
        _udpListener = new UdpClient(new IPEndPoint(IPAddress.Parse(config.IPAddress), config.Port));
    }

    public void Start()
    {
        // Make sure the server is not started twice
        if (_cts is not null) return;
        
        _logger.LogDebug("Starting server");
        _cts = new CancellationTokenSource();
        _tcpListener.Start();

        _tcpServerTask = TcpAcceptLoop(_cts.Token);
        _udpServerTask = UdpReceiveLoop(_cts.Token);
    }

    public async Task StopAsync()
    {
        var cts = _cts;
        if (cts is null) return;
        _cts = null;
        
        _logger.LogDebug("Stopping server");
        await cts.CancelAsync();
        _tcpListener.Stop();
        _udpListener.Close();

        await Task.WhenAll(
            _tcpServerTask ?? Task.CompletedTask,
            _udpServerTask ?? Task.CompletedTask
        );

        foreach (var connection in Connections.Values)
        {
            await connection.DisposeAsync();
        }
        
        Connections.Clear();
        cts.Dispose();
    }

    private async Task TcpAcceptLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            _logger.LogDebug("Waiting for a new TCP connection");
            TcpClient client;
            try
            {
                client = await _tcpListener.AcceptTcpClientAsync(ct);
                client.NoDelay = true;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) {
                break;
            }
            catch (ObjectDisposedException) when (ct.IsCancellationRequested) {
                break;
            }
            catch (SocketException) when (ct.IsCancellationRequested) {
                break;
            }
            
            var connection = new Connection(client, _udpListener, OnConnectionClosed)
            {
                Id = GenerateConnectionId()
            };

            connection.Start(ct);
            Connections.TryAdd(connection.Id, connection);
            
            _logger.LogInformation("Connection {ConnectionID} added", connection.Id);
        }
    }

    private async Task UdpReceiveLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            UdpReceiveResult message;
            try
            {
                _logger.LogDebug("Waiting for an UDP message");
                message = await _udpListener.ReceiveAsync(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) {
                break;
            }
            catch (ObjectDisposedException) when (ct.IsCancellationRequested) {
                break;
            }
            catch (SocketException) when (ct.IsCancellationRequested) {
                break;
            }
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
        Connections.TryRemove(connection.Id, out _);
        _ = connection.DisposeAsync();
    }
}