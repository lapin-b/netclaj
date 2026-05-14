using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using NetClajServer.Packets;
using NetClajServer.Packets.Claj;

namespace Benchmark;

class Program
{
    static void Main(string[] args)
    {
        BenchmarkRunner.Run<Benchmark>();
    }
}

[MemoryDiagnoser]
public class Benchmark
{
    private static readonly byte[] Payload = [
            0xfc, 0x03, 0x4a, 0x01, 0x42, 0xa1 
        ];
    
    [Benchmark]
    public MindustryPacket BenchmarkDeserializer()
    {
        return NetClajServer.Mindustry.Serializer.Deserialize(Payload);
    }
    
    [Benchmark]
    public ReadOnlyMemory<byte> BenchmarkSerializer()
    {
        return NetClajServer.Mindustry.Serializer.Serialize(new ConnectionJoinPacket
        {
            RoomId = long.MaxValue,
            ConnectionId = int.MaxValue
        });
    }
}