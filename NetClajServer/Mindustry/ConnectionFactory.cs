using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using NetClajServer.Datastructures;
using NetClajServer.Metrics;

namespace NetClajServer.Mindustry;

public class ConnectionFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ServerMetrics _metrics;

    public ConnectionFactory(ILoggerFactory loggerFactory, ServerMetrics metrics)
    {
        _loggerFactory = loggerFactory;
        _metrics = metrics;
    }

    public Connection Create(
        TcpClient tcpClient,
        UdpClient udpClient,
        SessionsManager sessionsManager,
        MindustryServer server,
        Func<int, bool> connectionIdExists
    )
    {
        int connectionId;

        do
        {
            connectionId = Random.Shared.Next(int.MinValue, int.MaxValue);
        } while (connectionIdExists(connectionId));

        return new Connection(
            connectionId,
            tcpClient,
            udpClient,
            server,
            sessionsManager,
            _loggerFactory.CreateLogger<Connection>(),
            _metrics
        );
    }
}