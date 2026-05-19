namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// NPC shop catalog sent to the buyer. rAthena
/// <c>clif_buylist</c> (clif.cpp:2229). Variable-length:
/// <c>00c6 &lt;packet_len&gt;.W { &lt;price&gt;.L &lt;discount&gt;.L
/// &lt;item_type&gt;.B &lt;name_id&gt;.W }*</c> — entry size = 11 bytes.
/// </summary>
public class ZC_PC_PURCHASE_ITEMLIST : OutgoingPacket
{
    private const int EntrySize = sizeof(int) + sizeof(int) + 1 + sizeof(ushort);

    public IReadOnlyList<ShopRow> Rows { get; init; } = Array.Empty<ShopRow>();

    public ZC_PC_PURCHASE_ITEMLIST() : base(PacketHeader.ZC_PC_PURCHASE_ITEMLIST, -1) { }

    public override int GetSize() => 2 + 2 + Rows.Count * EntrySize;

    public override void Write(BinaryWriter writer)
    {
        // WritePacket already wrote the header + packet_len (HasPacketLength
        // is true for var-len packets). Body only.
        foreach (var r in Rows)
        {
            writer.Write(r.Price);
            writer.Write(r.DiscountPrice);
            writer.Write(r.ItemType);
            writer.Write(r.NameId);
        }
    }

    public readonly record struct ShopRow(int Price, int DiscountPrice, byte ItemType, ushort NameId);
}
