using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace NetClajServer;

public class ClajServerService: IHostedService
{
    private readonly IConfiguration _configuration;
    private ClajServer? _server;

    public ClajServerService(
        IConfiguration configuration
    )
    {
        _configuration = configuration;
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var serverConfiguration = _configuration.GetSection("ClajServer").Get<ClajServerConfiguration>();
        if (serverConfiguration == null)
        {
            throw new Exception("No ClajServer configuration section found or invalid");
        }

        _server = new ClajServer(serverConfiguration);
        _server.Start();
        
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _server?.Close();
        return Task.CompletedTask;
    }
}