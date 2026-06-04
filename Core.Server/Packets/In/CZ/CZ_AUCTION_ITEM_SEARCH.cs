using System.Text;

namespace Core.Server.Packets.In.CZ;

/// <summary>
/// Browse/search auctions. rAthena <c>clif_parse_Auction_search</c> (clif.cpp, 0x0251). Fixed 32
/// bytes: <c>0251 &lt;type&gt;.W &lt;auctionId&gt;.L &lt;text&gt;.24 &lt;page&gt;.W</c>.
/// Search type: 0 armor, 1 weapon, 2 card, 3 misc, 4 name, 5 auction-id.
/// </summary>
public class CZ_AUCTION_ITEM_SEARCH : IncomingPacket
{
    private const int SIZE = 32;
    private const int TextLen = 24;

    public short Type { get; private set; }
    public uint AuctionId { get; private set; }
    public string Text { get; private set; } = string.Empty;
    public short Page { get; private set; }

    public CZ_AUCTION_ITEM_SEARCH() : base(PacketHeader.CZ_AUCTION_ITEM_SEARCH, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        Type = reader.ReadInt16();
        AuctionId = reader.ReadUInt32();
        var raw = reader.ReadBytes(TextLen);
        var zero = Array.IndexOf(raw, (byte)0);
        Text = Encoding.ASCII.GetString(raw, 0, zero < 0 ? raw.Length : zero);
        Page = reader.ReadInt16();
    }

    public static CZ_AUCTION_ITEM_SEARCH Create(BinaryReader reader)
    {
        var p = new CZ_AUCTION_ITEM_SEARCH();
        p.Read(reader);
        return p;
    }
}
