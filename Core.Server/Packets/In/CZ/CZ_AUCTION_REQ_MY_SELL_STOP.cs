namespace Core.Server.Packets.In.CZ;

/// <summary>
/// End an auction immediately — the seller's buy-now/stop. rAthena <c>clif_parse_Auction_close</c>
/// (clif.cpp, 0x025d). Fixed 6 bytes: <c>025d &lt;auctionId&gt;.L</c>. Drives the buy-now/close path
/// (the char side completes the sale to the high bidder).
/// </summary>
public class CZ_AUCTION_REQ_MY_SELL_STOP : IncomingPacket
{
    private const int SIZE = 6;

    public uint AuctionId { get; private set; }

    public CZ_AUCTION_REQ_MY_SELL_STOP() : base(PacketHeader.CZ_AUCTION_REQ_MY_SELL_STOP, SIZE) { }

    public override void Read(BinaryReader reader) => AuctionId = reader.ReadUInt32();

    public static CZ_AUCTION_REQ_MY_SELL_STOP Create(BinaryReader reader)
    {
        var p = new CZ_AUCTION_REQ_MY_SELL_STOP();
        p.Read(reader);
        return p;
    }
}
