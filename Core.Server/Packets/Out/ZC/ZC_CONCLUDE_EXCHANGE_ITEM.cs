namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// rAthena <c>clif_tradedeal_lock</c> (clif.cpp:4812) — notifies that
/// one side has pressed OK (locked its offer). Wire (0x00ec):
/// <c>&lt;who&gt;.B</c> — total 3 bytes.
///
/// <c>who</c>: 0 = self pressed OK, 1 = partner pressed OK.
/// </summary>
public class ZC_CONCLUDE_EXCHANGE_ITEM : OutgoingPacket
{
    private const int SIZE = 3;

    /// <summary>0 = self pressed OK, 1 = partner pressed OK.</summary>
    public byte Who { get; init; }

    public ZC_CONCLUDE_EXCHANGE_ITEM() : base(PacketHeader.ZC_CONCLUDE_EXCHANGE_ITEM, SIZE) { }

    public override void Write(BinaryWriter writer) => writer.Write(Who);
}
