namespace PacketHandling.Support;

public enum ClajConnectionCloseReason
{
    Error,
    Closed,
    ObsoleteClient,
    OutdatedServer,
    ServerClosed,
    ServerFull,
    Blacklisted,
    IdleTimeout,
    Ratelimited
}