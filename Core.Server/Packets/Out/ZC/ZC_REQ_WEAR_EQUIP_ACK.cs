namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// Server ack for an equip request. rAthena <c>clif_equipitemack</c>
/// (clif.cpp). Wire (PACKETVER_MAIN ≥ 20121205):
/// <c>0999 &lt;index&gt;.W &lt;wearLocation&gt;.L &lt;viewid&gt;.W &lt;result&gt;.B</c>
/// — total 11 bytes.
///
/// <para>
/// <see cref="Result"/> uses the modern code set
/// (<c>clif_equipitemack_flag</c>): 0 = OK, 1 = fail level, 2 = fail.
/// <see cref="WearLocation"/> is the EQP_* bitmask actually applied
/// (after server-side resolution for accessories / dual-wield).
/// </para>
/// </summary>
public class ZC_REQ_WEAR_EQUIP_ACK : OutgoingPacket
{
    private const int SIZE = 2 + sizeof(ushort) + sizeof(uint) + sizeof(ushort) + sizeof(byte);

    public ushort ClientIndex { get; init; }
    public uint WearLocation { get; init; }
    public ushort ViewId { get; init; }
    public byte Result { get; init; }

    public ZC_REQ_WEAR_EQUIP_ACK() : base(PacketHeader.ZC_REQ_WEAR_EQUIP_ACK, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(ClientIndex);
        writer.Write(WearLocation);
        writer.Write(ViewId);
        writer.Write(Result);
    }
}
