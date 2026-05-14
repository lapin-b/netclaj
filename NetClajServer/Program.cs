using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetClajServer.Claj;
using NetClajServer.Claj.Handlers;
using NetClajServer.Claj.PacketHandling;
using NetClajServer.Datastructures;
using NetClajServer.Mindustry;
using NetClajServer.Packets;
using NetClajServer.Packets.Claj;
using NetClajServer.Packets.Framework;
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
            // These handlers implement multiple interfaces and should point to the same object
            .AddSingleton<FrameworkPacketsHandler>()
            .AddSingleton<GamePacketHandler>()
            
            .AddSingleton<IPacketHandler<RoomClosureRequestPacket>, RoomCloseRequestHandler>()
            .AddSingleton<IPacketHandler<RoomCreationRequestPacket>, RoomCreateRequestHandler>()
            .AddSingleton<IPacketHandler<PingPacket>>(s => s.GetRequiredService<FrameworkPacketsHandler>())
            .AddSingleton<IPacketHandler<DiscoverHostPacket>>(s => s.GetRequiredService<FrameworkPacketsHandler>())
            .AddSingleton<IPacketHandler<KeepAlivePacket>>(s => s.GetRequiredService<FrameworkPacketsHandler>())
            .AddSingleton<IPacketHandler<RoomJoinPacket>, RoomJoinHandler>()
            .AddSingleton<IPacketHandler<ConnectionClosedPacket>, RoomLeaveHandler>()
            .AddSingleton<IPacketHandler<GamePacket>, GamePacketHandler>(s => s.GetRequiredService<GamePacketHandler>())
            .AddSingleton<IPacketHandler<ClajPayloadWrapping>, GamePacketHandler>(s => s.GetRequiredService<GamePacketHandler>())
            .AddSingleton<IPacketHandler<RoomConfigPacket>, RoomConfigPacketHandler>()
        ;

        collection.AddHostedService<ClajServerService>();
    }
}