namespace Core.Server.Packets.Out.ZC;

/// <summary>One cash-shop catalog entry: <c>ItemId</c> at <c>Price</c> cash points.</summary>
public readonly record struct CashItemEntry(uint ItemId, int Price);

/// <summary>
/// One cash-shop tab's item list. rAthena <c>clif_cashshop_list</c> (clif.cpp,
/// <c>PACKET_ZC_ACK_SCHEDULER_CASHITEM</c> 0x08ca) — emitted once per non-empty tab. Variable:
/// <c>08ca &lt;len&gt;.W &lt;count&gt;.W &lt;tabNum&gt;.W { &lt;itemId&gt;.L &lt;price&gt;.L }*</c>
/// (8 bytes per entry; the list begins at body offset 4).
/// </summary>
public class ZC_ACK_SCHEDULER_CASHITEM : OutgoingPacket
{
    private const int EntrySize = 8; // itemId.L(4) price.L(4)

    public short TabNum { get; init; }
    public IReadOnlyList<CashItemEntry> Items { get; init; } = Array.Empty<CashItemEntry>();

    public ZC_ACK_SCHEDULER_CASHITEM() : base(PacketHeader.ZC_ACK_SCHEDULER_CASHITEM, -1) { }

    public override int GetSize() => 2 + 2 + 2 + 2 + Items.Count * EntrySize; // header+len+count+tabNum+entries

    public override void Write(BinaryWriter writer)
    {
        writer.Write((short)Items.Count);
        writer.Write(TabNum);
        foreach (var it in Items)
        {
            writer.Write(it.ItemId);
            writer.Write(it.Price);
        }
    }
}
