namespace Core.Server.Packets.Out.ZC;

/// <summary>One row of a vending shop's price list (rAthena pre-20181121 form, 22 bytes).</summary>
public sealed class VendingListEntry
{
    public int Price { get; init; }
    public short Amount { get; init; }
    public short Index { get; init; }   // cart client index (server index + 2)
    public byte ItemType { get; init; }
    public short NameId { get; init; }
    public byte Identified { get; init; }
    public byte Damaged { get; init; }
    public byte Refine { get; init; }
    public short Card0 { get; init; }
    public short Card1 { get; init; }
    public short Card2 { get; init; }
    public short Card3 { get; init; }
}

/// <summary>
/// A vending shop's price list, sent to a buyer who clicked the stall. rAthena <c>clif_vendinglist</c>
/// (clif.cpp, 0x0133). Variable: <c>0133 &lt;len&gt;.W &lt;owner AID&gt;.L</c> then per item (22 bytes)
/// <c>price.L amount.W index.W type.B nameId.W identified.B damaged.B refine.B card0-3.W</c>.
/// </summary>
public class ZC_PC_PURCHASE_ITEMLIST_FROMMC : OutgoingPacket
{
    private const int EntrySize = 22;

    public uint OwnerAccountId { get; init; }
    public IReadOnlyList<VendingListEntry> Items { get; init; } = Array.Empty<VendingListEntry>();

    public ZC_PC_PURCHASE_ITEMLIST_FROMMC() : base(PacketHeader.ZC_PC_PURCHASE_ITEMLIST_FROMMC, -1) { }

    public override int GetSize() => 8 + Items.Count * EntrySize; // header(2) + len(2) + AID(4) + entries

    public override void Write(BinaryWriter writer)
    {
        writer.Write(OwnerAccountId);
        foreach (var it in Items)
        {
            writer.Write(it.Price);
            writer.Write(it.Amount);
            writer.Write(it.Index);
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
