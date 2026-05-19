namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// Inventory sellable to NPC. rAthena <c>clif_selllist</c>
/// (clif.cpp:2259). Variable-length:
/// <c>00c7 &lt;packet_len&gt;.W { &lt;index&gt;.W &lt;price&gt;.L &lt;overcharge_price&gt;.L }*</c>
/// — entry size = 10 bytes. <c>index</c> is the client-side slot
/// (server_index + 2). <c>overcharge_price</c> is the proceeds after
/// the rAthena vending-skill bonus (we send price == overcharge today).
/// </summary>
public class ZC_PC_SELL_ITEMLIST : OutgoingPacket
{
    private const int EntrySize = sizeof(ushort) + sizeof(int) + sizeof(int);

    public IReadOnlyList<SellRow> Rows { get; init; } = Array.Empty<SellRow>();

    public ZC_PC_SELL_ITEMLIST() : base(PacketHeader.ZC_PC_SELL_ITEMLIST, -1) { }

    public override int GetSize() => 2 + 2 + Rows.Count * EntrySize;

    public override void Write(BinaryWriter writer)
    {
        foreach (var r in Rows)
        {
            writer.Write(r.ClientIndex);
            writer.Write(r.Price);
            writer.Write(r.OverchargePrice);
        }
    }

    public readonly record struct SellRow(ushort ClientIndex, int Price, int OverchargePrice);
}
