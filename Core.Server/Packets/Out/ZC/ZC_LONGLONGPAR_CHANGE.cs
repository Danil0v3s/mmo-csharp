namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// rAthena <c>clif_longlongpar_change</c> ([clif.cpp:3618]). Used for SP
/// values that exceed int32 (modern <c>SP_BASEEXP</c> / <c>SP_JOBEXP</c>
/// at PACKETVER ≥ 20170830). Fixed 12 bytes:
/// <c>0x0acb packet_id (2) + varId (2) + value (8)</c>.
/// </summary>
public class ZC_LONGLONGPAR_CHANGE : OutgoingPacket
{
    private const int SIZE = sizeof(short) + sizeof(short) + sizeof(long);

    public ushort VarId { get; init; }
    public long Value { get; init; }

    public ZC_LONGLONGPAR_CHANGE() : base(PacketHeader.ZC_LONGLONGPAR_CHANGE, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(VarId);
        writer.Write(Value);
    }
}
