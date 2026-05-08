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
    public ConcurrentDictionary<int, Connection> Connections { get; } = new();
    public ConcurrentDictionary<long, Room> Rooms { get; } = new();
    public MindustryServer(ClajServerConfiguration config, ILogger<MindustryServer> logger, ILoggerProvider loggerProvider)
    {
        _logger = logger;
        _loggerProvider = loggerProvider;

        _tcpListener = new TcpListener(IPAddress.Parse(config.IPAddress), config.Port);
        _udpListener = new UdpClient(new IPEndPoint(IPAddress.Parse(config.IPAddress), config.Port));

        RegisterPacketHandler(new CreateClajRoomRequestHandler());
        RegisterPacketHandler(new CloseClajRoomRequestHandler());
        RegisterPacketHandler(new JoinClajRoomHandler());

        // Framework packets can be handled in their own grouped handler
        // since their respective handler is very short.
        var frameworkPacketsHandler = new FrameworkPacketsHandler();
        RegisterPacketHandler<PingPacket>(frameworkPacketsHandler);
        RegisterPacketHandler<KeepAlivePacket>(frameworkPacketsHandler);
        RegisterPacketHandler<DiscoverHostPacket>(frameworkPacketsHandler);
    }

    public void Start()
    {
        // Make sure the server is not started twice
        if (_cts is not null) return;
        
        _logger.LogInformation("Starting server");
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
        
        _logger.LogInformation("Stopping server");
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

        var context = new PacketContext
        {
            Server = this,
            Connection = connection,
            Logger = _loggerProvider.CreateLogger(packet.GetType().FullName!),
            CancellationToken = _cts.Token,
            IsTcp = isTcp
        };
        
        if (_router.TryGetValue(packet.GetType(), out var handler))
        {
            await handler(context, packet);
            return;
        }
        
        _logger.LogDebug("No handler for {packetType}", packet.GetType().Name);
    }

    public Room? FindConnectionInRooms(Connection connection)
    {
        return Rooms.Values.FirstOrDefault(r => r.HostConnectionId == connection.Id || r.HasPlayer(connection));
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
            TcpClient client;
            try
            {
                client = await _tcpListener.AcceptTcpClientAsync(ct);
                client.NoDelay = true;
            }
            catch (Exception e)
            {
                if (IsServerShutdownRequested(e, ct))
                {
                    break;
                }

                throw;
            }

            var connection = new Connection(
                client, 
                _udpListener, 
                this, 
                _loggerProvider.CreateLogger(nameof(Connection))
            );
            
            do
            {
                connection.Id = Random.Shared.Next(int.MinValue, int.MaxValue);
            } while (!Connections.TryAdd(connection.Id, connection));

            connection.Start(ct);
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
            catch (Exception e)
            {
                if (IsServerShutdownRequested(e, ct))
                {
                    break;
                }

                throw;
            }

            IMindustryPacket packet;
            try
            {
                packet = Serializer.Deserialize(message.Buffer);
            }
            // We don't care about a malformed packet, we just keep on going
            catch (Exception e)
            {
                if (e is ArgumentOutOfRangeException)
                {
                    continue;
                }
                
                _logger.LogWarning(e, "Exception while processing UDP packet");
                continue;
            }
            
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Received UDP bytes {buffer}", message.Buffer);
            }
            
            // Handle the register UDP packet here instead of looping through every connection and have them
            // handle it.
            if (packet is RegisterUdpPacket registerUdpPacket)
            {
                if (FrameworkPacketsHandler.TryRegisterUdpEndpoint(this, message.RemoteEndPoint, registerUdpPacket, out var connection))
                {
                    await connection.SendTcp(new RegisterUdpPacket { ConnectionId = 0 });
                }
                
                continue;
            }
            
            // TODO: Use a dictionary to quickly fetch a connection from the pile of active ones
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

    private static bool IsServerShutdownRequested(Exception e, CancellationToken ct)
    {
        if (
            e is not OperationCanceledException
            && e is not ObjectDisposedException
            && e is not SocketException
        )
        {
            return false;
        }

        return ct.IsCancellationRequested;
    }
}