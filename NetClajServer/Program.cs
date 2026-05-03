using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace NetClajServer;

class Program
{
    static void Main(string[] args)
    {
        var hostBuilder = Host.CreateApplicationBuilder(args);
        hostBuilder.Services.AddHostedService<ClajServerService>();
        var host = hostBuilder.Build();
        host.Run();
    }
}