using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetClajServer.Claj.Handlers;
using NetClajServer.Claj.PacketHandling;
using NetClajServer.Datastructures;
using NetClajServer.Mindustry;
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
            .AddSingleton<RawPacketHandler>()
            
            .AddSingleton<IPacketHandler<RoomCloseRequestPacket>, CloseClajRoomRequestHandler>()
            .AddSingleton<IPacketHandler<RoomCreateRequestPacket>, CreateClajRoomRequestHandler>()
            .AddSingleton<IPacketHandler<PingPacket>>(s => s.GetRequiredService<FrameworkPacketsHandler>())
            .AddSingleton<IPacketHandler<DiscoverHostPacket>>(s => s.GetRequiredService<FrameworkPacketsHandler>())
            .AddSingleton<IPacketHandler<KeepAlivePacket>>(s => s.GetRequiredService<FrameworkPacketsHandler>())
            .AddSingleton<IPacketHandler<RoomJoinPacket>, JoinClajRoomHandler>()
            .AddSingleton<IPacketHandler<ConnectionClosedPacket>, LeaveClajRoomHandler>()
            .AddSingleton<IPacketHandler<GamePacket>, RawPacketHandler>(s => s.GetRequiredService<RawPacketHandler>())
            .AddSingleton<IPacketHandler<ClajPayloadWrapping>, RawPacketHandler>(s => s.GetRequiredService<RawPacketHandler>())
        ;

        collection.AddHostedService<ClajServerService>();
    }
}