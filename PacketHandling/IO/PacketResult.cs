namespace NetClajServer.Packets.IO;

public readonly record struct PacketResult(
    PacketErrorCode Code,
    string PacketName,
    string Field,
    long Offset,
    string? Detail
)
{
    public bool IsSuccess => Code == PacketErrorCode.Success;
    public bool IsFailure => Code != PacketErrorCode.Success;

    public static PacketResult Ok() =>
        new()
        {
            Code = PacketErrorCode.Success,
            PacketName = string.Empty,
            Detail = null,
            Field = string.Empty,
            Offset = -1
        };

    public static PacketResult Err(
        PacketErrorCode code,
        string packetName,
        string field,
        long offset,
        string? detail = null
    ) => new(code, packetName, field, offset, detail);
}