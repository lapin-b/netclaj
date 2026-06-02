using Microsoft.Extensions.Hosting;

namespace NetClajServer.Metrics;

public class ProcessedPacketsRateUpdater: BackgroundService
{
    private const int UpdateInterval = 1;
    private readonly ServerMetrics _metrics;

    public ProcessedPacketsRateUpdater(ServerMetrics metrics)
    {
        _metrics = metrics;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var timer = new PeriodicTimer(new TimeSpan(0, 0, UpdateInterval));
        
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            _metrics.UpdatePacketsRate(UpdateInterval);
        }
    }
}