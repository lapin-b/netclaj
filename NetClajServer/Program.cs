using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetClajServer.Claj;
using NetClajServer.Claj.Handlers;
using NetClajServer.Claj.PacketHandling;
using NetClajServer.Datastructures;
using NetClajServer.Metrics;
using NetClajServer.Mindustry;
using NetClajServer.Packets;
using PacketHandling.Claj;
using PacketHandling.Framework;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace NetClajServer;

class Program
{
    static void Main(string[] args)
    {
        var hostBuilder = Host.CreateApplicationBuilder(args);

        using var defaultLogger = new LoggerConfiguration()
            .WriteTo.Console(
                outputTemplate: "{Timestamp:o} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}",
                theme: AnsiConsoleTheme.Sixteen
            )
            .ReadFrom.Configuration(hostBuilder.Configuration)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("System", LogEventLevel.Information)
            .Destructure.ByTransforming<Connection>(c => c.Id)
            .Destructure.ByTransforming<Room>(r => r.Id)
            .CreateLogger();
        Log.Logger = defaultLogger;
        
        ConfigureServices(hostBuilder.Services);
        
        var host = hostBuilder.Build();
        host.Run();
    }

    static void ConfigureServices(IServiceCollection collection)
    {
        collection
            .AddLogging(builder => builder
                .ClearProviders()
                .AddSerilog()
            )
            .AddSingleton<RoomFactory>()
            .AddSingleton<ConnectionFactory>()
            .AddSingleton<MindustryServer>()
            .AddSingleton<ClajServerConfiguration>(services =>
            {
                var config = services.GetRequiredService<IConfiguration>()
                    .GetRequiredSection("ClajServer")
                    .Get<ClajServerConfiguration>();

                return config ?? throw new Exception("Can't read ClajServer configuration");
            })
            .AddSingleton<ServerMetrics>()
            .AddSingleton<SessionsManager>()
            
            // These handlers implement multiple interfaces and should point to the same object
            .AddSingleton<FrameworkPacketsHandler>()
            .AddSingleton<GamePacketHandler>()
            .AddSingleton<RoomJoinHandler>()

            // arc.net Framework packets
            .AddSingleton<IPacketHandler<PingPacket>>(s => s.GetRequiredService<FrameworkPacketsHandler>())
            .AddSingleton<IPacketHandler<DiscoverHostPacket>>(s => s.GetRequiredService<FrameworkPacketsHandler>())
            .AddSingleton<IPacketHandler<KeepAlivePacket>>(s => s.GetRequiredService<FrameworkPacketsHandler>())            
            
            // Rooms configuration handling
            .AddSingleton<IPacketHandler<RoomCreationRequestPacket>, RoomCreateRequestHandler>()
            .AddSingleton<IPacketHandler<RoomClosureRequestPacket>, RoomCloseRequestHandler>()
            .AddSingleton<IPacketHandler<RoomConfigPacket>, RoomConfigPacketHandler>()
            .AddSingleton<IPacketHandler<RoomStatePacket>, RoomStateHandler>()
            .AddSingleton<IPacketHandler<RoomListRequestPacket>, RoomListRequestHandler>()
            
            // Room joining and quitting
            .AddSingleton<IPacketHandler<RoomJoinRequestPacket>>(s => s.GetRequiredService<RoomJoinHandler>())
            .AddSingleton<IPacketHandler<RoomJoinPacket>>(s => s.GetRequiredService<RoomJoinHandler>())
            .AddSingleton<IPacketHandler<ConnectionClosedPacket>, RoomLeaveHandler>()
            
            // Relay
            .AddSingleton<IPacketHandler<GamePacket>, GamePacketHandler>(s => s.GetRequiredService<GamePacketHandler>())
            .AddSingleton<IPacketHandler<ClajPayloadWrapping>, GamePacketHandler>(s => s.GetRequiredService<GamePacketHandler>())
        ;

        collection.AddHostedService<ClajServerService>();
        collection.AddHostedService<ProcessedPacketsRateUpdater>();
    }
}