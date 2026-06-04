namespace Core.Server.Packets.Out.ZC;

/// <summary>One row of a buying store's offer list (9 bytes, rAthena pre-20181121 form).</summary>
public sealed class BuyingStoreEntry
{
    public int Price { get; init; }
    public short Amount { get; init; }
    public byte ItemType { get; init; }
    public short NameId { get; init; }
}

/// <summary>
/// A buying store's offer list, sent to its owner on open. rAthena <c>clif_buyingstore_myitemlist</c>
/// (clif.cpp, 0x0813). Variable: <c>0813 &lt;len&gt;.W &lt;AID&gt;.L &lt;zeny limit&gt;.L</c> then per offer
/// (9 bytes) <c>price.L amount.W type.B nameId.W</c>.
/// </summary>
public class ZC_MYITEMLIST_BUYING_STORE : OutgoingPacket
{
    private const int EntrySize = 9;

    public uint AccountId { get; init; }
    public int ZenyLimit { get; init; }
    public IReadOnlyList<BuyingStoreEntry> Items { get; init; } = Array.Empty<BuyingStoreEntry>();

    public ZC_MYITEMLIST_BUYING_STORE() : base(PacketHeader.ZC_MYITEMLIST_BUYING_STORE, -1) { }

    public override int GetSize() => 12 + Items.Count * EntrySize; // header(2)+len(2)+AID(4)+limit(4)+entries

    public override void Write(BinaryWriter writer)
    {
        writer.Write(AccountId);
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
