namespace NetClajServer.Claj;

public class RoomConfiguration
{
    public bool IsPublic { get; set; }
    public bool IsProtectedByPin { get; set; }
    public bool CanRequestHostState { get; set; }
    
    public short? Pin { get; set; }
    public short MaxClients { get; set; }
}