namespace NetClajServer.Mindustry;

public class SerializerException: Exception
{
    public const string FamilyClaj = "CLaJ";
    public const string FamilyFramework = "framework";

    public string PacketFamily { get; init; }
    
    public SerializerException(string argument, string packetCategory, object value): 
        base($"Couldn't parse value {value} from variable {argument} for category {packetCategory}")
    {
        PacketFamily = packetCategory;
    }

    public SerializerException(string message) : base(message)
    {
        PacketFamily = "(packet processing)";
    }
}