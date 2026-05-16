namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// "This entity started walking." Broadcast to viewers in range. rAthena
/// <c>clif_move</c>. Shape: 0x0086 packet_id (2) + entityId (4) + movePos (6) + startTime (4) = 16 bytes.
/// </summary>
public class ZC_NOTIFY_MOVE : OutgoingPacket
{
    private const int SIZE = 16;

    public int EntityId { get; init; }
    public short FromX { get; init; }
    public short FromY { get; init; }
    public short ToX { get; init; }
    public short ToY { get; init; }
    public uint StartTime { get; init; }

    public ZC_NOTIFY_MOVE() : base(PacketHeader.ZC_NOTIFY_MOVE, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(EntityId);
        PositionPacker.WriteMove(writer, FromX, FromY, ToX, ToY);
        writer.Write(StartTime);
    }
}
