namespace PacketHandling.Support;

public record RoomListItem(long Id, bool IsProtectedByPin, ReadOnlyMemory<byte> State);