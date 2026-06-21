namespace PacketHandling.IO;

public enum PacketErrorCode
{
    Success,
    UnexpectedEof,
    InvalidValue,
    LimitExceeded
}