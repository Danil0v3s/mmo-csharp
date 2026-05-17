namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// rAthena <c>clif_longpar_change</c> ([clif.cpp:3556]). Same shape as
/// <see cref="ZC_PAR_CHANGE"/> (0x00B0) but routed to the "long" var-set
/// (zeny, exp on pre-PACKETVER ≥ 20170830 clients). Fixed 8 bytes.
/// </summary>
public class ZC_LONGPAR_CHANGE : OutgoingPacket
{
    private const int SIZE = sizeof(short) + sizeof(short) + sizeof(int);

    public ushort VarId { get; init; }
    public int Value { get; init; }

    public ZC_LONGPAR_CHANGE() : base(PacketHeader.ZC_LONGPAR_CHANGE, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(VarId);
        writer.Write(Value);
    }
}
