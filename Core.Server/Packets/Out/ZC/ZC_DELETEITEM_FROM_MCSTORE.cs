namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// Tell the vendor an item was bought from their shop. rAthena <c>clif_vendingreport</c> (clif.cpp,
/// 0x0137 legacy form). Fixed 6 bytes: <c>0137 &lt;index&gt;.W &lt;amount&gt;.W</c>.
/// </summary>
public class ZC_DELETEITEM_FROM_MCSTORE : OutgoingPacket
{
    private const int SIZE = 2 + 2 + 2; // 6

    public short Index { get; init; }   // cart client index
    public short Amount { get; init; }

    public ZC_DELETEITEM_FROM_MCSTORE() : base(PacketHeader.ZC_DELETEITEM_FROM_MCSTORE, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(Index);
        writer.Write(Amount);
    }
}
