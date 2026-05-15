using Microsoft.Extensions.Logging;
using NetClajServer.Mindustry;

namespace NetClajServer.Claj;

public class RoomFactory
{
    private readonly ILoggerFactory _loggerFactory;

    public RoomFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    public Room Create(Connection host, string roomType, Func<long, bool> roomIdExists)
    {
        long roomId;

        do
        {
            roomId = Random.Shared.NextInt64();
        } while (roomIdExists(roomId));
        
        return new Room(roomId, host, roomType, _loggerFactory.CreateLogger<Room>());
    }
}