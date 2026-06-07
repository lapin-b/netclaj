namespace NetClajServer;

public class Constants
{
    public const int ConnectionRawPacketsBuffer = 8;
    public const int ClajServerVersion = 4;
    public static TimeSpan RoomStateFreshnessDuration = new(0, 0, 30);
    public const int RoomStateQueryGlobalTimeout = 10000; // ms
    public const int RoomStateQueryTimeout = 5; // seconds
}