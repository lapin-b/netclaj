using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetClajServer.Claj;
using NetClajServer.Claj.Handlers;
using NetClajServer.Claj.PacketHandling;
using NetClajServer.Datastructures;
using NetClajServer.Packets;
using NetClajServer.Packets.Claj;
using NetClajServer.Packets.Framework;

namespace NetClajServer.Mindustry;

public class MindustryServer
{
    private readonly ILogger<MindustryServer> _logger;
    private readonly ILoggerProvider _loggerProvider;

    // Packet routing
    private readonly Dictionary<Type, Func<PacketContext, MindustryPacket, Task>> _router = new();

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
    private ConcurrentDictionary<IPEndPoint, int> _udpEndPointsToConnection = new();
    public MindustryServer(
        ClajServerConfiguration config, 
        ILogger<MindustryServer> logger, 
        ILoggerProvider loggerProvider,
        IServiceProvider provider
        )
    {
        _logger = logger;
        _loggerProvider = loggerProvider;

        _tcpListener = new TcpListener(IPAddress.Parse(config.IPAddress), config.Port);
        _udpListener = new UdpClient(new IPEndPoint(IPAddress.Parse(config.IPAddress), config.Port));

        MapPacketHandlers<RoomCreateRequestPacket>(provider);
        MapPacketHandlers<RoomCloseRequestPacket>(provider);
        MapPacketHandlers<PingPacket>(provider);
        MapPacketHandlers<DiscoverHostPacket>(provider);
        MapPacketHandlers<KeepAlivePacket>(provider);
        MapPacketHandlers<RoomJoinPacket>(provider);
        MapPacketHandlers<ConnectionClosedPacket>(provider);
        MapPacketHandlers<GamePacket>(provider);
        MapPacketHandlers<ClajPayloadWrapping>(provider);
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
            await connection.CloseAsync();
        }
        
        Connections.Clear();
        cts.Dispose();
    }

    public async Task HandleMindustryPacket(Connection connection, MindustryPacket packet)
    {
        if (_cts is null) throw new InvalidOperationException("Server is not started");

        var context = new PacketContext
        {
            Server = this,
            Connection = connection,
            Logger = _loggerProvider.CreateLogger(packet.GetType().FullName!),
            CancellationToken = _cts.Token,
            IsTcp = packet.IsTcp
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

    public async Task NotifyConnectionClosure(Connection connection, ConnectionCloseReason? reason)
    {
        // Remove the connection from the registry
        _logger.LogInformation("Connection {ConnectionId} closed. Reason={Reason}", connection.Id, reason);
        Connections.TryRemove(connection.Id, out _);
        
        if (connection.UdpEndpoint is { } endpoint)
        {
            _udpEndPointsToConnection.TryRemove(endpoint, out _);
        }
        
        // Remove this client from the room or close it, depending on who it is
        if (connection.ParticipatesInRoomId is { } participatesInRoomId
            && Rooms.TryGetValue(participatesInRoomId, out var room)
           )
        {
            if (room.HostConnectionId == connection.Id)
            {
                _logger.LogInformation("Closing room");
                await room.Close();
                Rooms.TryRemove(room.Id, out _);
            }
            else
            {
                _logger.LogInformation("Leaving room");
                await room.TryLeaveRoom(connection, false, true);
            }
        }
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

                _logger.LogError(e, "And UDP decided to party");
                throw;
            }

            MindustryPacket packet;
            try
            {
                // An UDP packet has no length prefix, no need to strip it from the buffer
                packet = Serializer.Deserialize(message.Buffer);
                packet.IsTcp = false;
            }
            // We don't care about a malformed packet, we just keep on going
            catch (SerializerException)
            {
                // no-op
                continue;
            }
            catch (Exception e)
            {
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
                // Binding an UDP endpoint to a TCP connection many times is unsupported by design. The original
                // network engine (`arc.net` in Mindustry) doesn't seem to support this case.
                if (FrameworkPacketsHandler.TryRegisterUdpEndpoint(this, message.RemoteEndPoint, registerUdpPacket, out var connection))
                {
                    _udpEndPointsToConnection.TryAdd(message.RemoteEndPoint, connection.Id);
                    await connection.SendTcp(new RegisterUdpPacket { ConnectionId = 0 });
                }
                
                continue;
            }

            if (
                !_udpEndPointsToConnection.TryGetValue(message.RemoteEndPoint, out var connectionId)
                || !Connections.TryGetValue(connectionId, out var fromConnection)
            )
            {
                _logger.LogWarning("Endpoint {endpoint} has no corresponding connection", message.RemoteEndPoint);
                continue;
            }

            await fromConnection.ProcessDeserializedPacket(packet);
        }
        
        _udpListener.Close();
    }

    private void MapPacketHandlers<TPacket>(IServiceProvider provider)
        where TPacket : MindustryPacket
    {
        // Caching a delegate calling a handler like this is fine because they're all singletons.
        var handler = provider.GetRequiredService<IPacketHandler<TPacket>>();
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