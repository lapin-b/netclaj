using System.Diagnostics;

namespace StressTestHarness;

class Program
{
    private const int ClientsCount = 10;
    private const int RoomsCount = 1;

    static async Task Main(string[] args)
    {
        MindustryClient.GeneratedPacketsPerSecond = 5;
        MindustryClient.GeneratedPacketsJitter = .3;
        
        Debug.Assert(ClientsCount >= RoomsCount);
        
        var globalCancel = new CancellationTokenSource();
        Console.CancelKeyPress += new ConsoleCancelEventHandler(HandleCancellation(globalCancel));
        
        var clients = new List<MindustryClient>();
        var roomIds = new List<long>();

        Console.WriteLine("Connecting and handshaking");
        for (var i = 0; i < ClientsCount; i++)
        {
            var client = new MindustryClient("127.0.0.1", 7000, globalCancel.Token);
            await client.ExecuteHandshake();
            clients.Add(client);
        }

        Console.WriteLine("Creating rooms");
        await Task.WhenAll(clients[..RoomsCount].Select(h => h.CreateRoom()));
        roomIds = clients.Where(c => c.IsHost).Select(c => (long)c.RoomId!).ToList();
        
        Console.WriteLine("Joining rooms");
        foreach (var client in clients.Where(c => c.IsClient))
        {
            var roomId = roomIds[Random.Shared.Next(0, roomIds.Count)];
            Console.WriteLine($"Joining {client.ConnectionId} into {roomId}");
            await client.JoinRoom(roomId);
        }

        foreach (var client in clients)
        {
            client.Run();
        }
        
        globalCancel.Token.WaitHandle.WaitOne();
        Console.WriteLine("Closing connections");
        foreach (var client in clients)
        {
            client.Stop();
        }
    }

    static Action<object?, ConsoleCancelEventArgs> HandleCancellation(CancellationTokenSource globalCancel)
    {
        return (sender, args) =>
        {
            Console.WriteLine("Cancelling");
            globalCancel.Cancel();
        };
    }
}