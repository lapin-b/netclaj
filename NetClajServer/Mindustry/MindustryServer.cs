using System.Net;
using System.Diagnostics;
using System.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetClajServer.Claj.PacketHandling;
using NetClajServer.Datastructures;
using NetClajServer.Metrics;
using PacketHandling;
using PacketHandling.Claj;
using PacketHandling.Framework;
using PacketHandling.Serialization;

namespace NetClajServer.Mindustry;

public class MindustryServer
{
    private readonly ILogger<MindustryServer> _logger;
    private readonly SessionsManager _sessionsManager;
    private readonly ServerMetrics _metrics;

    // Packet routing
    private readonly Dictionary<Type, Func<PacketContext, MindustryPacket, ValueTask>> _router = new();

    // Connection accepting
    private readonly TcpListener _tcpListener;
    private readonly UdpClient _udpListener;
    private Task? _tcpServerTask;
    private Task? _udpServerTask;

    private Stopwatch _udpWatch = new();

    // The cancellation token source is set when the server starts and
    // unset when the server stops. The downstream code shouldn't get
    // a null value.
    private CancellationTokenSource? _cts;

    public MindustryServer(
        ClajServerConfiguration config, 
        ILogger<MindustryServer> logger,
        SessionsManager sessionsManager,
        IServiceProvider provider,
        ServerMetrics metrics
    )
    {
        _logger = logger;
        _sessionsManager = sessionsManager;
        _metrics = metrics;

        _tcpListener = new TcpListener(IPAddress.Parse(config.IPAddress), config.Port);
        _udpListener = new UdpClient(new IPEndPoint(IPAddress.Parse(config.IPAddress), config.Port));

        // arc.net handlers
        MapPacketHandlers<PingPacket>(provider);
        MapPacketHandlers<DiscoverHostPacket>(provider);
        MapPacketHandlers<KeepAlivePacket>(provider);

        // Rooms configuration handling
        MapPacketHandlers<RoomCreationRequestPacket>(provider);
        MapPacketHandlers<RoomClosureRequestPacket>(provider);
        MapPacketHandlers<RoomConfigPacket>(provider);
        MapPacketHandlers<RoomStatePacket>(provider);
        MapPacketHandlers<RoomListRequestPacket>(provider);
        
        // Room joining and quitting
        MapPacketHandlers<RoomJoinRequestPacket>(provider);
        MapPacketHandlers<RoomJoinPacket>(provider);
        MapPacketHandlers<ConnectionClosedPacket>(provider);
        
        // Relay
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

        // TODO: Implement an "unhandled exception" handler on the tasks if they ever go faulted.
        // They should log what happened and either kill the server or restart the task
        // https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task.continuewith#system-threading-tasks-task-continuewith(system-action((system-threading-tasks-task-system-object))-system-object-system-threading-tasks-taskcontinuationoptions)
        _tcpServerTask = TcpAcceptLoop(_cts.Token);
        _udpServerTask = UdpReceiveLoop(_cts.Token);
    }

    public async Task StopAsync()
    {
        var cts = _cts;
        if (cts is null) return;
        _cts = null;
        
        _logger.LogInformation("Stopping server");
        cts.Cancel();
        _tcpListener.Stop();
        _udpListener.Close();
        
        await _sessionsManager.CloseAllConnections();
        await Task.WhenAll(
            _tcpServerTask ?? Task.CompletedTask,
            _udpServerTask ?? Task.CompletedTask
        );
        
        cts.Dispose();
        _tcpListener.Dispose();
        _udpListener.Dispose();
    }

    public ValueTask HandleMindustryPacket(Connection connection, MindustryPacket packet)
    {
        if (_cts is null) throw new InvalidOperationException("Server is not started");

        var context = new PacketContext
        {
            Connection = connection,
            CancellationToken = _cts.Token,
        };
        
        if (_router.TryGetValue(packet.GetType(), out var handler))
        {
            return handler(context, packet);
        }
        
        _logger.LogDebug("No handler for {packetType}", packet.GetType().Name);
        return ValueTask.CompletedTask;
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
            catch (SocketException e)
            {
                _logger.LogError(e, "SocketException while accepting a connection");
                break;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("TCP: Server shutdown requested");
                break;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while accepting connections");
                break;
            }
            
            var connection = _sessionsManager.CreateAndStartConnection(
                client,
                _udpListener,
                this,
                ct
            );
            
            _logger.LogInformation("Client {@Connection} connected", connection);
            
            var task = connection.SendTcp(new RegisterTcpPacket { ConnectionId = connection.Id });
            if (task.IsCompletedSuccessfully) continue;
            await task;
        }
    }

    private async Task UdpReceiveLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            UdpReceiveResult message;
            MindustryPacket packet;

            try
            {
                message = await _udpListener.ReceiveAsync(ct);
                _udpWatch.Start();
                // An UDP packet has no length prefix, no need to strip it from the buffer
                packet = Serializer.Deserialize(message.Buffer);
                packet.TransportIsTcp = false;
                
                // Handle the register UDP packet here instead of looping through every connection and have them
                // handle it.
                if (packet is RegisterUdpPacket registerUdpPacket)
                {
                    // Binding an UDP endpoint to a TCP connection many times is unsupported by design. The original
                    // network engine (`arc.net` in Mindustry) doesn't seem to support this case.
                    if (_sessionsManager.TryRegisterUdpEndpointToConnection(message.RemoteEndPoint, registerUdpPacket.ConnectionId, out var connection))
                    {
                        var sendRegisterUdpTask = connection.SendTcp(new RegisterUdpPacket { ConnectionId = 0 });
                        if (sendRegisterUdpTask.IsCompletedSuccessfully) continue;
                        await sendRegisterUdpTask;
                    }
                
                    _udpWatch.Stop();
                    _metrics.PacketProcessHistogram.Record(_udpWatch.ElapsedMilliseconds);
                    _udpWatch.Reset();

                    continue;
                }

                if (_sessionsManager.GetConnectionByUdpEndpoint(message.RemoteEndPoint) is not { } fromConnection)
                {
                    _udpWatch.Stop();
                    _metrics.PacketProcessHistogram.Record(_udpWatch.Elapsed.TotalMilliseconds);
                    _udpWatch.Reset();
                    
                    continue;
                }

                var processingTask = fromConnection.ProcessDeserializedPacket(packet);
                if (!processingTask.IsCompletedSuccessfully)
                {
                    await processingTask;
                }
                
                _udpWatch.Stop();
                _metrics.PacketProcessHistogram.Record(_udpWatch.Elapsed.TotalMilliseconds);
                _udpWatch.Reset();
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("UDP: Server shutdown requested");
                break;
            }
            catch (SocketException e) when (e.SocketErrorCode is SocketError.ConnectionReset or SocketError.ConnectionAborted)
            {
                // no-op
            }
            catch (SocketException e)
            {
                _logger.LogWarning(e, "Error while processing an UDP payload");
            }
            // We don't care about a malformed packet, we just keep on going
            catch (SerializerException)
            {
                // no-op
            }
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
}