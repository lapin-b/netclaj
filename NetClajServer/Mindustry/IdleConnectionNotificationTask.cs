using Microsoft.Extensions.Hosting;

namespace NetClajServer.Mindustry;

public class IdleConnectionNotificationTask: BackgroundService
{
    private readonly SessionsManager _sessionsManager;

    public IdleConnectionNotificationTask(SessionsManager sessionsManager)
    {
        _sessionsManager = sessionsManager;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(new TimeSpan(0, 0, 0, 0, 100));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            foreach (var room in _sessionsManager.Rooms.Values)
            {
                if (room.PlayersCount == 0) continue;
                _ = room.SendIdleConnectionsNotification().AsTask();
            }
        }
    }
}