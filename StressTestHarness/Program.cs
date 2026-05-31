using System.Diagnostics;
using System.IO.Pipes;

namespace StressTestHarness;

class Program
{
    private const int ClientsCount = 200;
    private const int RoomsCount = 10;
    private const double RotationRate = .1;

    private const string HostToTest = "127.0.0.1";
    private const int HostPortTotest = 7000;

    private static readonly CancellationTokenSource GlobalCancel = new();
    private static readonly List<MindustryClient> Clients = [];
    private static readonly List<long> RoomIds = [];
    
    static async Task Main(string[] args)
    {
        MindustryClient.GeneratedPacketsPerSecond = 5;
        MindustryClient.GeneratedPacketsJitter = .3;
        
        Debug.Assert(ClientsCount >= RoomsCount);
        Console.CancelKeyPress += new ConsoleCancelEventHandler(HandleCancellation());
        
        Console.WriteLine("Connecting and handshaking");
        for (var i = 0; i < ClientsCount; i++)
        {
            var client = await CreateConnection(HostToTest, HostPortTotest, i < RoomsCount);
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
        Console.WriteLine("Closing connections (and it's fine if we can't clean everything I guess)");
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
                var newConnection = await CreateConnection(HostToTest, HostPortTotest, false);
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