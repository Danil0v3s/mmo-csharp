namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// A buying store's offer list, sent to a visitor who clicked it. rAthena <c>clif_buyingstore_itemlist</c>
/// (clif.cpp, 0x0818). Variable: <c>0818 &lt;len&gt;.W &lt;AID&gt;.L &lt;store id&gt;.L &lt;zeny limit&gt;.L</c>
/// then per offer (9 bytes) <c>price.L amount.W type.B nameId.W</c> (reuses <see cref="BuyingStoreEntry"/>).
/// </summary>
public class ZC_ACK_ITEMLIST_BUYING_STORE : OutgoingPacket
{
    private const int EntrySize = 9;

    public uint AccountId { get; init; }
    public uint StoreId { get; init; }
    public int ZenyLimit { get; init; }
    public IReadOnlyList<BuyingStoreEntry> Items { get; init; } = Array.Empty<BuyingStoreEntry>();

    public ZC_ACK_ITEMLIST_BUYING_STORE() : base(PacketHeader.ZC_ACK_ITEMLIST_BUYING_STORE, -1) { }

    public override int GetSize() => 16 + Items.Count * EntrySize; // header(2)+len(2)+AID(4)+storeId(4)+limit(4)+entries

    public override void Write(BinaryWriter writer)
    {
        writer.Write(AccountId);
        writer.Write(StoreId);
        writer.Write(ZenyLimit);
        foreach (var it in Items)
        {
            writer.Write(it.Price);
            writer.Write(it.Amount);
            writer.Write(it.ItemType);
            writer.Write(it.NameId);
        }
    }
}
