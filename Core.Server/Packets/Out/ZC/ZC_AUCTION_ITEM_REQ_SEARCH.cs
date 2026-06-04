using System.Text;

namespace Core.Server.Packets.Out.ZC;

/// <summary>One auction row in a browse-results list (rAthena <c>auction_data</c> → the 83-byte
/// wire entry). Cards default to 0 until char-side full-item fidelity lands (GP-AUCTION turn 2).</summary>
public sealed class AuctionResultEntry
{
    public uint AuctionId { get; init; }
    public string SellerName { get; init; } = string.Empty;
    public ushort NameId { get; init; }
    public int Type { get; init; }
    public ushort Amount { get; init; } = 1;
    public bool Identified { get; init; } = true;
    public byte Attribute { get; init; }   // "damaged"
    public byte Refine { get; init; }
    public ushort Card0 { get; init; }
    public ushort Card1 { get; init; }
    public ushort Card2 { get; init; }
    public ushort Card3 { get; init; }
    public int Price { get; init; }
    public int BuyNow { get; init; }
    public string BuyerName { get; init; } = string.Empty;
    public uint DeleteTime { get; init; }  // expiry unix timestamp
}

/// <summary>
/// Auction browse/search results. rAthena <c>clif_Auction_results</c> (clif.cpp, 0x0252). Variable:
/// <c>0252 &lt;len&gt;.W &lt;pages&gt;.L &lt;count&gt;.L { entry }*</c>, 83 bytes per entry —
/// <c>auctionId.L sellerName.24 nameId.W type.L amount.W identified.B damaged.B refine.B
/// card0.W card1.W card2.W card3.W price.L buynow.L buyerName.24 deleteTime.L</c>.
/// </summary>
public class ZC_AUCTION_ITEM_REQ_SEARCH : OutgoingPacket
{
    private const int EntrySize = 83;
    private const int NameLen = 24;

    public int Pages { get; init; }
    public IReadOnlyList<AuctionResultEntry> Auctions { get; init; } = Array.Empty<AuctionResultEntry>();

    public ZC_AUCTION_ITEM_REQ_SEARCH() : base(PacketHeader.ZC_AUCTION_ITEM_REQ_SEARCH, -1) { }

    public override int GetSize() => 2 + 2 + 4 + 4 + Auctions.Count * EntrySize; // header+len+pages+count+entries

    public override void Write(BinaryWriter writer)
    {
        writer.Write(Pages);
        writer.Write(Auctions.Count);
        foreach (var a in Auctions)
        {
            writer.Write(a.AuctionId);
            WriteFixedString(writer, a.SellerName, NameLen);
            writer.Write(a.NameId);
            writer.Write(a.Type);
            writer.Write(a.Amount);
            writer.Write((byte)(a.Identified ? 1 : 0));
            writer.Write(a.Attribute);
            writer.Write(a.Refine);
            writer.Write(a.Card0);
            writer.Write(a.Card1);
            writer.Write(a.Card2);
            writer.Write(a.Card3);
            writer.Write(a.Price);
            writer.Write(a.BuyNow);
            WriteFixedString(writer, a.BuyerName, NameLen);
            writer.Write(a.DeleteTime);
        }
    }

    private static void WriteFixedString(BinaryWriter writer, string s, int len)
    {
        var buf = new byte[len];
        var bytes = Encoding.ASCII.GetBytes(s ?? string.Empty);
        Array.Copy(bytes, buf, Math.Min(bytes.Length, len - 1)); // leave a null terminator
        writer.Write(buf);
    }
}
