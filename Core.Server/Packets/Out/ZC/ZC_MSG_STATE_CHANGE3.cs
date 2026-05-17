namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// rAthena <c>clif_status_change3</c>. Status icon notification with
/// duration tick. Fixed 29 bytes:
/// <c>0x0983 packet_id (2) + index (2) + AID (4) + state (1) + tick (4)
///   + total tick (4) + val1 (4) + val2 (4) + val3 (4)</c>.
/// </summary>
public class ZC_MSG_STATE_CHANGE3 : OutgoingPacket
{
    private const int SIZE = 29;

    public short Index { get; init; }
    public uint AccountId { get; init; }
    public byte State { get; init; }
    public uint Tick { get; init; }
    public uint TotalTick { get; init; }
    public int Val1 { get; init; }
    public int Val2 { get; init; }
    public int Val3 { get; init; }

    public ZC_MSG_STATE_CHANGE3() : base(PacketHeader.ZC_MSG_STATE_CHANGE3, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(Index);
        writer.Write(AccountId);
        writer.Write(State);
        writer.Write(Tick);
        writer.Write(TotalTick);
        writer.Write(Val1);
        writer.Write(Val2);
        writer.Write(Val3);
    }
}
