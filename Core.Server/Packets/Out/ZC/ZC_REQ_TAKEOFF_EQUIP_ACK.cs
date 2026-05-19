namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// Server ack for an unequip request. rAthena <c>clif_unequipitemack</c>.
/// Wire (PACKETVER ≥ 20130000):
/// <c>099a &lt;index&gt;.W &lt;wearLocation&gt;.L &lt;flag&gt;.B</c> — total 9 bytes.
///
/// <para>
/// <see cref="Flag"/> is 1 on success / 0 on failure (legacy bool ack —
/// the modern OK=0/FAIL=2 enum is only used by the wear-ack packet).
/// </para>
/// </summary>
public class ZC_REQ_TAKEOFF_EQUIP_ACK : OutgoingPacket
{
    private const int SIZE = 2 + sizeof(ushort) + sizeof(uint) + sizeof(byte);

    public ushort ClientIndex { get; init; }
    public uint WearLocation { get; init; }
    public byte Flag { get; init; }

    public ZC_REQ_TAKEOFF_EQUIP_ACK() : base(PacketHeader.ZC_REQ_TAKEOFF_EQUIP_ACK, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(ClientIndex);
        writer.Write(WearLocation);
        writer.Write(Flag);
    }
}
