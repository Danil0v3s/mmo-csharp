namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// rAthena <c>e_auction_result</c> message flag carried by <see cref="ZC_AUCTION_RESULT"/>
/// (<c>clif_Auction_message</c>). The client renders the matching system message.
/// </summary>
public enum AuctionResultMessage : byte
{
    BidFail = 0,            // You have failed to bid into the auction
    BidSuccess = 1,        // Bid success
    Cancelled = 2,         // The auction has been canceled
    SellComplete = 3,      // Auction ended / item sold
    CharServerError = 4,   // No char server
    NotEnoughZenyFee = 5,  // Not enough zeny to pay the auction fee
    BuyComplete = 6,       // Bought / closed
    NoBidderToBuy = 7,     // Cannot buy-now (no bidder)
    NotEnoughZenyBid = 8,  // Not enough zeny to bid
}

/// <summary>
/// An auction status-code message. rAthena <c>clif_Auction_message</c> (clif.cpp, 0x0250). Fixed 3
/// bytes: <c>0250 &lt;flag&gt;.B</c>.
/// </summary>
public class ZC_AUCTION_RESULT : OutgoingPacket
{
    private const int SIZE = 3;

    public AuctionResultMessage Flag { get; init; }

    public ZC_AUCTION_RESULT() : base(PacketHeader.ZC_AUCTION_RESULT, SIZE) { }

    public override void Write(BinaryWriter writer) => writer.Write((byte)Flag);
}
