namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// A vendor's own shop list, sent to the vendor when their stall opens. rAthena <c>clif_openvending</c>
/// (clif.cpp, 0x0136 legacy form). Variable: <c>0136 &lt;len&gt;.W &lt;owner id&gt;.L</c> then per item
/// (22 bytes) <c>price.L index.W amount.W type.B nameId.W identified.B damaged.B refine.B card0-3.W</c>.
/// Identical fields to <see cref="ZC_PC_PURCHASE_ITEMLIST_FROMMC"/> (reuses <see cref="VendingListEntry"/>)
/// but with the index/amount order swapped, matching the rAthena MYITEMLIST sub-struct.
/// </summary>
public class ZC_PC_PURCHASE_MYITEMLIST : OutgoingPacket
{
    private const int EntrySize = 22;

    public uint OwnerId { get; init; }
    public IReadOnlyList<VendingListEntry> Items { get; init; } = Array.Empty<VendingListEntry>();

    public ZC_PC_PURCHASE_MYITEMLIST() : base(PacketHeader.ZC_PC_PURCHASE_MYITEMLIST, -1) { }

    public override int GetSize() => 8 + Items.Count * EntrySize; // header(2) + len(2) + ownerId(4) + entries

    public override void Write(BinaryWriter writer)
    {
        writer.Write(OwnerId);
        foreach (var it in Items)
        {
            writer.Write(it.Price);
            writer.Write(it.Index);   // MYITEMLIST: index before amount
            writer.Write(it.Amount);
            writer.Write(it.ItemType);
            writer.Write(it.NameId);
            writer.Write(it.Identified);
            writer.Write(it.Damaged);
            writer.Write(it.Refine);
            writer.Write(it.Card0);
            writer.Write(it.Card1);
            writer.Write(it.Card2);
            writer.Write(it.Card3);
        }
    }
}
