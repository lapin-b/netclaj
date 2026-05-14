namespace NetClajServer.Packets.IO;

public enum PacketErrorCode
{
    Success,
    UnexpectedEof,
    InvalidValue,
    LimitExceeded
}