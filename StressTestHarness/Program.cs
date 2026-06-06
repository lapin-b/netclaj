using Microsoft.Extensions.Configuration;

namespace StressTestHarness;

class Program
{
    private static int _clientsCount;
    private static int _roomsCount;
    private const double RotationRate = 0.1;

    private static string _hostToTest = "127.0.0.1";
    private static int _hostPortTotest = 7000;

    private static readonly CancellationTokenSource GlobalCancel = new();
    private static readonly List<MindustryClient> Clients = [];
    private static readonly List<long> RoomIds = [];
    
    static async Task Main(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";

        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", true)
            .AddJsonFile($"appsettings.{environment}.json", true)
            .Build();

        _hostToTest = configuration.GetValue<string>("Host") ?? 
                     throw new Exception("Missing [Host] configuration key");
        _hostPortTotest = configuration.GetValue<int?>("Port") ?? 
                         throw new Exception("Missing [Port] configuration key");
        var stressProfile = args[0];
        var profileConfiguration = configuration
            .GetRequiredSection("StressProfiles")
            .GetSection(stressProfile)
            .Get<StressConfiguration>();

        if (profileConfiguration == null)
        {
            Console.WriteLine($"Stress configuration profile {stressProfile} doesn't exist or has an invalid shape");
            return;
        }

        if (profileConfiguration.Clients < profileConfiguration.Rooms)
        {
            Console.WriteLine("Rooms count cannot be larger than clients count");
            return;
        }

        _clientsCount = profileConfiguration.Clients;
        _roomsCount = profileConfiguration.Rooms;
        
        Console.CancelKeyPress += new ConsoleCancelEventHandler(HandleCancellation());
        
        Console.WriteLine("Connecting and handshaking");
        for (var i = 0; i < _clientsCount; i++)
        {
            var client = await CreateConnection(_hostToTest, _hostPortTotest, i < _roomsCount);
            Clients.Add(client);

            if (client.IsHost)
            {
                RoomIds.Add((long)client.RoomId!);
                client.Run();
            }
        }

        Console.WriteLine("Joining rooms");
        foreach (var client in Clients.Where(c => c.IsClient))
        {
            var roomId = RoomIds[Random.Shared.Next(0, RoomIds.Count)];
            await client.JoinRoom(roomId);
            client.Run();
            
            Console.WriteLine($"Joined {client.ConnectionId} into {roomId}");
        }

        _ = RotateClientRoomsLoop();
        
        GlobalCancel.Token.WaitHandle.WaitOne();
        Console.WriteLine("Closing connections");
        foreach (var client in Clients)
        {
            client.Stop();
        }
    }

    static async Task RotateClientRoomsLoop()
    {
        GlobalCancel.Token.ThrowIfCancellationRequested();
        var ct = GlobalCancel.Token;
        
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(10000, ct);
            var nonHostClients = Clients.Where(c => c.IsClient).ToList();
            var connectionCountToRotate = (int)Math.Ceiling(nonHostClients.Count * RotationRate);

            if (connectionCountToRotate == 0) continue;

            var victims = new HashSet<MindustryClient>(connectionCountToRotate);
            do
            {
                victims.Add(Random.Shared.RandomItemInList(nonHostClients)!);
            } while (victims.Count < connectionCountToRotate);

            foreach (var connection in victims)
            {
                ct.ThrowIfCancellationRequested();

                connection.Stop();
                var newConnection = await CreateConnection(_hostToTest, _hostPortTotest, false);
                var roomIdToJoin = Random.Shared.RandomItemInList(RoomIds);
                await newConnection.JoinRoom(roomIdToJoin);

                Console.WriteLine($"Switched connection {connection.ConnectionId} to join room {roomIdToJoin} as {newConnection.ConnectionId}");
                
                Clients.Remove(connection);
                Clients.Add(newConnection);

                await Task.Delay(Random.Shared.Next(200, 300), ct);
            }
        }
    }

    static async Task<MindustryClient> CreateConnection(string host, int port, bool isRoomHost)
    {
        var connection = new MindustryClient(host, port, GlobalCancel.Token);
        await connection.ExecuteHandshake();

        if (isRoomHost)
        {
            Console.WriteLine($"{connection.ConnectionId} creating room");
            await connection.CreateRoom();
        }

        return connection;
    }

    private static Action<object?, ConsoleCancelEventArgs> HandleCancellation()
    {
        return (sender, args) =>
        {
            Console.WriteLine("Global cancelling");
            GlobalCancel.Cancel();
        };
    }
}