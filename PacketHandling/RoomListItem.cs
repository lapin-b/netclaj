namespace PacketHandling;

public record RoomListItem(long Id, bool IsProtectedByPin, ReadOnlyMemory<byte> State);