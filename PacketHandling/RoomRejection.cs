namespace PacketHandling;

public enum RoomRejection: byte
{
    Error,
    ServerFull,
    ServerClosing,
    NotFound,
    RoomFull,
    PinRequired,
    InvalidPin,
    Incompatible,
    
    Success = 255,
}