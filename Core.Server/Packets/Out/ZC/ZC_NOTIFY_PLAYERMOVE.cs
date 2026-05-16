namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// "Walk accepted" echo, sent only to the moving player. rAthena
/// <c>clif_walkok</c>. Shape: 0x0087 packet_id (2) + startTime (4) + movePos (6) = 12 bytes.
///
/// movePos is the 6-byte WBUFPOS2 packing (start cell → end cell + sub-cell offsets).
/// </summary>
public class ZC_NOTIFY_PLAYERMOVE : OutgoingPacket
{
    private const int SIZE = 12;

    public uint StartTime { get; init; }
    public short FromX { get; init; }
    public short FromY { get; init; }
    public short ToX { get; init; }
    public short ToY { get; init; }

    public ZC_NOTIFY_PLAYERMOVE() : base(PacketHeader.ZC_NOTIFY_PLAYERMOVE, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(StartTime);
        PositionPacker.WriteMove(writer, FromX, FromY, ToX, ToY);
    }
}
