using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using NetClajServer.Claj;
using NetClajServer.Claj.Handlers;
using NetClajServer.Claj.PacketHandling;
using NetClajServer.Datastructures;
using NetClajServer.Packets;
using NetClajServer.Packets.Framework;

namespace NetClajServer.Mindustry;

public class MindustryServer
{
    private readonly ILogger<MindustryServer> _logger;
    private readonly ILoggerProvider _loggerProvider;
    
    // Packet routing
    private readonly Dictionary<Type, Func<PacketContext, IMindustryPacket, Task>> _router = new();

    // Connection accepting
    private readonly TcpListener _tcpListener;
    private readonly UdpClient _udpListener;
    private Task? _tcpServerTask;
    private Task? _udpServerTask;

    // The cancellation token source is set when the server starts and
    // unset when the server stops. The downstream code shouldn't get
    // a null value.
    private CancellationTokenSource? _cts;

    // Active connections and rooms management
    public ConcurrentDictionary<long, Connection> Connections { get; } = new();
    public ConcurrentDictionary<long, Room> Rooms { get; } = new();
 
    public MindustryServer(ClajServerConfiguration config, ILogger<MindustryServer> logger, ILoggerProvider loggerProvider)
    {
        _logger = logger;
        _loggerProvider = loggerProvider;

        _tcpListener = new TcpListener(IPAddress.Parse(config.IPAddress), config.Port);
        _udpListener = new UdpClient(new IPEndPoint(IPAddress.Parse(config.IPAddress), config.Port));
        
        RegisterPacketHandler(new CreateClajRoomRequestHandler());
        RegisterPacketHandler(new CloseClajRoomRequestHandler());
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
            connection.Close();
        }
        
        Connections.Clear();
        cts.Dispose();
    }

    public async Task HandleMindustryPacket(Connection connection, IMindustryPacket packet, bool isTcp)
    {
        if (_cts is null) throw new InvalidOperationException("Server is not started");

        // Handle framework packets directly in the server since they're unrelated to claj
        switch (packet)
        {
            case PingPacket ping:
                await connection.Send(new PingPacket()
                {
                    Id = ping.Id,
                    IsReply = true
                }, isTcp);

                return;
            case DiscoverHostPacket:
                await connection.Send(new DiscoverHostPacket(), isTcp);
                return;
            case KeepAlivePacket:
                await connection.Send(new KeepAlivePacket(), isTcp);
                return;
        }

        var context = new PacketContext
        {
            Server = this,
            Connection = connection,
            Logger = _loggerProvider.CreateLogger(packet.GetType().FullName!),
            CancellationToken = _cts.Token
        };
        
        if (_router.TryGetValue(packet.GetType(), out var handler))
        {
            await handler(context, packet);
            return;
        }
        
        _logger.LogDebug("No handler for {packetType}", packet.GetType().Name);
    }

    public void NotifyConnectionClosure(Connection connection, ConnectionCloseReason? reason)
    {
        _logger.LogInformation("Connection {ConnectionId} closed. Reason={Reason}", connection.Id, reason);
        Connections.TryRemove(connection.Id, out _);
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
            
            var connection = new Connection(client, _udpListener, this, _loggerProvider.CreateLogger(nameof(Connection)))
            {
                Id = GenerateConnectionId()
            };

            connection.Start(ct);
            Connections.TryAdd(connection.Id, connection);
            _logger.LogInformation("Client ID {ConnectionID} connected", connection.Id);
            await connection.SendTcp(new RegisterTcpPacket { ConnectionId = connection.Id });
        }
    }

    private async Task UdpReceiveLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            UdpReceiveResult message;
            try
            {
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

            IMindustryPacket packet;
            try
            {
                packet = Serializer.Deserialize(message.Buffer);
            }
            // We don't care about a malformed packet, we just keep on going
            catch (Exception e)
            {
                if (e is ArgumentOutOfRangeException or EndOfStreamException)
                {
                    continue;
                }
                
                _logger.LogWarning(e, "Exception while processing UDP packet");
                continue;
            }
            
            // Handle the register UDP packet here instead of looping through every connection and have them
            // handle it.
            if (packet is RegisterUdpPacket registerUdpPacket)
            {
                var connection = Connections.Values.FirstOrDefault(c => c.Id == registerUdpPacket.ConnectionId);
                if (connection is null || connection.UdpEndpoint != null) continue;
                connection.UdpEndpoint = message.RemoteEndPoint;
                await connection.SendTcp(new RegisterUdpPacket { ConnectionId = 0 });
                continue;
            }
            
            var fromConnection = Connections
                .Values
                .FirstOrDefault(c => c.UdpEndpoint != null && c.UdpEndpoint.Equals(message.RemoteEndPoint));

            if (fromConnection != null)
            {
                await HandleMindustryPacket(fromConnection, packet, false);
            }
        }
        
        _udpListener.Close();
    }

    private void RegisterPacketHandler<TPacket>(IPacketHandler<TPacket> handler)
        where TPacket : IMindustryPacket
    {
        _router[typeof(TPacket)] = (context, packet) => handler.HandleAsync(context, (TPacket)packet);
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
}