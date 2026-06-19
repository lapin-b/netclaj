using System.Diagnostics.Metrics;

namespace NetClajServer.Metrics;

public class ServerMetrics
{
    public ObservableCounter<long> PacketsProcessed { get; private init;  }
    public ObservableGauge<long> PacketsPerSecond { get; private init; }
    
    public Histogram<long> PacketProcessHistogram { get; private init; }

    private long _totalPacketsProcessed = 0;
    private long _lastTotalPacketsProcessed = 0;
    private int _packetsPerSecond = 0; 
    
    public ServerMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("NetClajServer.Server");
        
        PacketsProcessed = meter.CreateObservableCounter(
            "Packets processed",
            () => Volatile.Read(ref _totalPacketsProcessed),
            "packets"
        );
        
        PacketsPerSecond = meter.CreateObservableGauge<long>(
            "Packets/s",
            () => _packetsPerSecond,
            "pps"
        );

        PacketProcessHistogram = meter.CreateHistogram<long>("Time to process", "ms", "Time to process a packet");
    }

    public void IncrementIncomingPacketsProcessed()
    {
        Interlocked.Increment(ref _totalPacketsProcessed);
    }

    public void UpdatePacketsRate(int updateInterval)
    {
        var currentTotal = Volatile.Read(ref _totalPacketsProcessed);
        _packetsPerSecond = (int)(currentTotal - _lastTotalPacketsProcessed) / updateInterval;
        _lastTotalPacketsProcessed = currentTotal;
    }
}