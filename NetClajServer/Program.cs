using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetClajServer.Mindustry;
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
        ;

        collection.AddHostedService<ClajServerService>();
    }
}