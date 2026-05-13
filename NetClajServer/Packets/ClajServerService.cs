using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetClajServer.Datastructures;
using NetClajServer.Mindustry;

namespace NetClajServer.Packets;

public class ClajServerService: IHostedService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ClajServerService> _logger;
    private MindustryServer _server;

    public ClajServerService(
        IConfiguration configuration,
        MindustryServer server,
        ILogger<ClajServerService> logger
    )
    {
        _configuration = configuration;
        _server = server;
        _logger = logger;
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var serverConfiguration = _configuration.GetSection("ClajServer").Get<ClajServerConfiguration>();
        if (serverConfiguration == null)
        {
            throw new Exception("No ClajServer configuration section found or invalid");
        }

        _logger.LogInformation("Starting CLaJ server");
        _server.Start();
        
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping CLaJ server");
        await _server.StopAsync();
    }
}