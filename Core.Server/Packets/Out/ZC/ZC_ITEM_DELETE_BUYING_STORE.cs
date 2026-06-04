namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// Remove a sold item from the seller's inventory display (sent to the seller). rAthena
/// <c>clif_buyingstore_delete_item</c> (clif.cpp, 0x081c). Fixed 10 bytes:
/// <c>081c &lt;index&gt;.W &lt;amount&gt;.W &lt;price&gt;.L</c> — price per item (the client totals the zeny).
/// </summary>
public class ZC_ITEM_DELETE_BUYING_STORE : OutgoingPacket
{
    private const int SIZE = 2 + 2 + 2 + 4; // 10

    public short Index { get; init; }   // seller inventory client index
    public short Amount { get; init; }
    public int Price { get; init; }

    public ZC_ITEM_DELETE_BUYING_STORE() : base(PacketHeader.ZC_ITEM_DELETE_BUYING_STORE, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(Index);
        writer.Write(Amount);
        writer.Write(Price);
    }
}
