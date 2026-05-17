namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// rAthena <c>clif_par_change</c> ([clif.cpp:3544]). Per-stat broadcast
/// — emitted from <c>clif_updatestatus</c> for SPs whose value fits in
/// an int32 (most basic stats: HP, MaxHP, weight, hit, flee, etc.).
///
/// Fixed 8 bytes: <c>0x00b0 packet_id (2) + varId (2) + value (4)</c>.
/// </summary>
public class ZC_PAR_CHANGE : OutgoingPacket
{
    private const int SIZE = sizeof(short) + sizeof(short) + sizeof(int);

    public ushort VarId { get; init; }
    public int Value { get; init; }

    public ZC_PAR_CHANGE() : base(PacketHeader.ZC_PAR_CHANGE, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(VarId);
        writer.Write(Value);
    }
}
