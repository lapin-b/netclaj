using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace NetClajServer.Mindustry;

public class ConnectionFactory
{
    private readonly ILoggerFactory _loggerFactory;

    public ConnectionFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    public Connection Create(
        TcpClient tcpClient,
        UdpClient udpClient,
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
            _loggerFactory.CreateLogger<Connection>()
        );
    }
}